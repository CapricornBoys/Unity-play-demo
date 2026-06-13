using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Match3
{
    /// <summary>
    /// 三消游戏控制器。
    /// 负责生成棋盘、相邻交换、匹配检测、消除、下落、补充和连锁计分。
    /// </summary>
    public sealed class Match3Panel : UIPanel
    {
        private const int Rows = 8;
        private const int Columns = 8;
        private const int TileTypeCount = 6;
        private const float TileSize = 92f;
        private const float TileSpacing = 8f;
        private const float StepDuration = 0.14f;

        private readonly int[,] board = new int[Rows, Columns];
        private readonly Match3TileView[,] views = new Match3TileView[Rows, Columns];
        private readonly List<Vector2Int> matches = new List<Vector2Int>();
        private readonly HashSet<int> matchKeys = new HashSet<int>();

        private RectTransform boardRoot;
        private RectTransform tileTemplate;
        private Text scoreText;
        private Text movesText;
        private Text statusText;
        private Vector2Int selectedCell = new Vector2Int(-1, -1);
        private bool processing;
        private int score;
        private int moves;

        private readonly Color[] tileColors =
        {
            new Color(0.95f, 0.25f, 0.28f),
            new Color(0.2f, 0.55f, 0.98f),
            new Color(0.24f, 0.82f, 0.4f),
            new Color(0.98f, 0.72f, 0.18f),
            new Color(0.68f, 0.3f, 0.92f),
            new Color(0.98f, 0.42f, 0.72f)
        };

        protected override void OnInitialize()
        {
            GetUI<Button>("CloseButton").onClick.AddListener(Close);
            GetUI<Button>("RestartButton").onClick.AddListener(RestartGame);

            boardRoot = GetUI<RectTransform>("BoardRoot");
            tileTemplate = GetUI<RectTransform>("TileTemplate");
            scoreText = GetUI<Text>("ScoreText");
            movesText = GetUI<Text>("MovesText");
            statusText = GetUI<Text>("StatusText");

            tileTemplate.gameObject.SetActive(false);
            CreateTileViews();
            RestartGame();
        }

        /// <summary>
        /// 接收棋子点击。首次点击选中，第二次点击相邻棋子时尝试交换。
        /// </summary>
        public void SelectTile(int row, int column)
        {
            if (processing)
            {
                return;
            }

            Vector2Int clicked = new Vector2Int(column, row);
            if (selectedCell.x < 0)
            {
                selectedCell = clicked;
                statusText.text = "Select an adjacent tile";
                RefreshBoard();
                return;
            }

            if (selectedCell == clicked)
            {
                ClearSelection();
                return;
            }

            if (!AreAdjacent(selectedCell, clicked))
            {
                selectedCell = clicked;
                statusText.text = "Select an adjacent tile";
                RefreshBoard();
                return;
            }

            Vector2Int first = selectedCell;
            selectedCell = new Vector2Int(-1, -1);
            StartCoroutine(TrySwapRoutine(first, clicked));
        }

        private void RestartGame()
        {
            if (processing)
            {
                return;
            }

            StopAllCoroutines();
            score = 0;
            moves = 0;
            selectedCell = new Vector2Int(-1, -1);
            GenerateBoardWithoutInitialMatches();
            RefreshBoard();
            UpdateHud("Match three or more tiles");
        }

        private void CreateTileViews()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    RectTransform clone = Instantiate(tileTemplate, boardRoot);
                    clone.name = $"Tile_{row}_{column}";
                    clone.gameObject.SetActive(true);

                    Match3TileView view =
                        clone.gameObject.AddComponent<Match3TileView>();
                    view.Initialize(this);
                    views[row, column] = view;
                }
            }
        }

        /// <summary>
        /// 生成棋盘时排除横向或纵向连续三个相同类型，避免开局自动消除。
        /// </summary>
        private void GenerateBoardWithoutInitialMatches()
        {
            do
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int column = 0; column < Columns; column++)
                    {
                        int type;
                        do
                        {
                            type = Random.Range(0, TileTypeCount);
                        }
                        while (CreatesInitialMatch(row, column, type));

                        board[row, column] = type;
                    }
                }
            }
            while (!HasPossibleMove());
        }

        private bool CreatesInitialMatch(int row, int column, int type)
        {
            bool horizontal = column >= 2
                              && board[row, column - 1] == type
                              && board[row, column - 2] == type;
            bool vertical = row >= 2
                            && board[row - 1, column] == type
                            && board[row - 2, column] == type;
            return horizontal || vertical;
        }

        private IEnumerator TrySwapRoutine(Vector2Int first, Vector2Int second)
        {
            processing = true;
            SwapBoardValues(first, second);
            RefreshBoard();
            yield return new WaitForSecondsRealtime(StepDuration);

            FindAllMatches();
            if (matches.Count == 0)
            {
                // 交换后没有形成匹配，恢复原来的位置。
                SwapBoardValues(first, second);
                RefreshBoard();
                UpdateHud("No match. Swap reverted.");
                processing = false;
                yield break;
            }

            moves++;
            int chain = 0;
            while (matches.Count > 0)
            {
                chain++;
                score += matches.Count * 10 * chain;
                RemoveMatches();
                RefreshBoard();
                UpdateHud(chain > 1 ? $"Chain x{chain}!" : "Matched!");
                yield return new WaitForSecondsRealtime(StepDuration);

                CollapseColumns();
                RefreshBoard();
                yield return new WaitForSecondsRealtime(StepDuration);

                FillEmptyCells();
                RefreshBoard();
                yield return new WaitForSecondsRealtime(StepDuration);
                FindAllMatches();
            }

            if (!HasPossibleMove())
            {
                GenerateBoardWithoutInitialMatches();
                RefreshBoard();
                UpdateHud("No moves. Board shuffled.");
            }
            else
            {
                UpdateHud("Match three or more tiles");
            }
            processing = false;
        }

        /// <summary>
        /// 检查棋盘中是否至少存在一次能够形成匹配的相邻交换。
        /// </summary>
        private bool HasPossibleMove()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (column + 1 < Columns
                        && SwapCreatesMatch(row, column, row, column + 1))
                    {
                        return true;
                    }

                    if (row + 1 < Rows
                        && SwapCreatesMatch(row, column, row + 1, column))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SwapCreatesMatch(
            int firstRow,
            int firstColumn,
            int secondRow,
            int secondColumn)
        {
            int temporary = board[firstRow, firstColumn];
            board[firstRow, firstColumn] = board[secondRow, secondColumn];
            board[secondRow, secondColumn] = temporary;

            bool createsMatch =
                HasMatchAt(firstRow, firstColumn)
                || HasMatchAt(secondRow, secondColumn);

            temporary = board[firstRow, firstColumn];
            board[firstRow, firstColumn] = board[secondRow, secondColumn];
            board[secondRow, secondColumn] = temporary;
            return createsMatch;
        }

        private bool HasMatchAt(int row, int column)
        {
            int type = board[row, column];
            if (type < 0)
            {
                return false;
            }

            int horizontalCount = 1;
            for (int x = column - 1; x >= 0 && board[row, x] == type; x--)
            {
                horizontalCount++;
            }
            for (int x = column + 1; x < Columns && board[row, x] == type; x++)
            {
                horizontalCount++;
            }

            if (horizontalCount >= 3)
            {
                return true;
            }

            int verticalCount = 1;
            for (int y = row - 1; y >= 0 && board[y, column] == type; y--)
            {
                verticalCount++;
            }
            for (int y = row + 1; y < Rows && board[y, column] == type; y++)
            {
                verticalCount++;
            }

            return verticalCount >= 3;
        }

        private void FindAllMatches()
        {
            matches.Clear();
            matchKeys.Clear();

            // 扫描每一行中的连续相同棋子。
            for (int row = 0; row < Rows; row++)
            {
                int runStart = 0;
                for (int column = 1; column <= Columns; column++)
                {
                    bool continues = column < Columns
                                     && board[row, column] >= 0
                                     && board[row, column] == board[row, runStart];
                    if (continues)
                    {
                        continue;
                    }

                    int runLength = column - runStart;
                    if (board[row, runStart] >= 0 && runLength >= 3)
                    {
                        for (int x = runStart; x < column; x++)
                        {
                            AddMatch(row, x);
                        }
                    }

                    runStart = column;
                }
            }

            // 扫描每一列中的连续相同棋子。
            for (int column = 0; column < Columns; column++)
            {
                int runStart = 0;
                for (int row = 1; row <= Rows; row++)
                {
                    bool continues = row < Rows
                                     && board[row, column] >= 0
                                     && board[row, column] == board[runStart, column];
                    if (continues)
                    {
                        continue;
                    }

                    int runLength = row - runStart;
                    if (board[runStart, column] >= 0 && runLength >= 3)
                    {
                        for (int y = runStart; y < row; y++)
                        {
                            AddMatch(y, column);
                        }
                    }

                    runStart = row;
                }
            }
        }

        private void AddMatch(int row, int column)
        {
            int key = row * Columns + column;
            if (matchKeys.Add(key))
            {
                matches.Add(new Vector2Int(column, row));
            }
        }

        private void RemoveMatches()
        {
            for (int i = 0; i < matches.Count; i++)
            {
                Vector2Int cell = matches[i];
                board[cell.y, cell.x] = -1;
            }
        }

        /// <summary>
        /// 每列从下向上压缩，空位全部移动到顶部。
        /// </summary>
        private void CollapseColumns()
        {
            for (int column = 0; column < Columns; column++)
            {
                int writeRow = Rows - 1;
                for (int row = Rows - 1; row >= 0; row--)
                {
                    if (board[row, column] < 0)
                    {
                        continue;
                    }

                    board[writeRow, column] = board[row, column];
                    if (writeRow != row)
                    {
                        board[row, column] = -1;
                    }
                    writeRow--;
                }

                while (writeRow >= 0)
                {
                    board[writeRow, column] = -1;
                    writeRow--;
                }
            }
        }

        private void FillEmptyCells()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (board[row, column] < 0)
                    {
                        board[row, column] = Random.Range(0, TileTypeCount);
                    }
                }
            }
        }

        private void SwapBoardValues(Vector2Int first, Vector2Int second)
        {
            int temporary = board[first.y, first.x];
            board[first.y, first.x] = board[second.y, second.x];
            board[second.y, second.x] = temporary;
        }

        private static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        private void ClearSelection()
        {
            selectedCell = new Vector2Int(-1, -1);
            statusText.text = "Match three or more tiles";
            RefreshBoard();
        }

        private void RefreshBoard()
        {
            float stride = TileSize + TileSpacing;
            float boardWidth = Columns * TileSize + (Columns - 1) * TileSpacing;
            float boardHeight = Rows * TileSize + (Rows - 1) * TileSpacing;
            boardRoot.sizeDelta = new Vector2(boardWidth, boardHeight);

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    Match3TileView view = views[row, column];
                    int type = board[row, column];
                    bool selected = selectedCell.x == column && selectedCell.y == row;

                    Color color = type >= 0 ? tileColors[type] : Color.clear;
                    view.gameObject.SetActive(type >= 0);
                    view.Bind(row, column, Mathf.Max(0, type), color, selected);
                    view.SetBoardPosition(new Vector2(
                        column * stride,
                        -row * stride));
                }
            }
        }

        private void UpdateHud(string status)
        {
            scoreText.text = $"Score: {score}";
            movesText.text = $"Moves: {moves}";
            statusText.text = status;
        }
    }
}
