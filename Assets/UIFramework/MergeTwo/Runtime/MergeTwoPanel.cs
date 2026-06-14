using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.MergeTwo
{
    /// <summary>
    /// Controls a merge-two board. Equal adjacent tiles merge into the next value.
    /// </summary>
    public sealed class MergeTwoPanel : UIPanel
    {
        private const int Rows = 6;
        private const int Columns = 6;
        private const int InitialTileCount = 10;
        private const float TileSize = 112f;
        private const float TileSpacing = 12f;
        private const float StepDuration = 0.12f;

        private readonly int[,] board = new int[Rows, Columns];
        private readonly MergeTwoTileView[,] views =
            new MergeTwoTileView[Rows, Columns];

        private readonly Color[] tileColors =
        {
            new Color(0.35f, 0.72f, 0.98f),
            new Color(0.28f, 0.86f, 0.58f),
            new Color(0.98f, 0.76f, 0.24f),
            new Color(0.98f, 0.48f, 0.24f),
            new Color(0.92f, 0.28f, 0.42f),
            new Color(0.72f, 0.34f, 0.92f),
            new Color(0.38f, 0.3f, 0.92f),
            new Color(0.2f, 0.68f, 0.78f)
        };

        private RectTransform boardRoot;
        private RectTransform tileTemplate;
        private Text scoreText;
        private Text movesText;
        private Text statusText;
        private Vector2Int selectedCell = new Vector2Int(-1, -1);
        private bool processing;
        private bool gameOver;
        private int score;
        private int moves;
        private int highestLevel;

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

        public void SelectTile(int row, int column)
        {
            if (processing || gameOver)
            {
                return;
            }

            Vector2Int clicked = new Vector2Int(column, row);
            bool clickedIsEmpty = board[row, column] < 0;
            if (selectedCell.x < 0)
            {
                if (!clickedIsEmpty)
                {
                    selectedCell = clicked;
                    UpdateHud("Choose an adjacent space or equal tile");
                    RefreshBoard();
                }
                return;
            }

            if (selectedCell == clicked)
            {
                ClearSelection();
                return;
            }

            if (!AreAdjacent(selectedCell, clicked))
            {
                selectedCell = clickedIsEmpty
                    ? new Vector2Int(-1, -1)
                    : clicked;
                UpdateHud(clickedIsEmpty
                    ? "Select a tile first"
                    : "Choose an adjacent space or equal tile");
                RefreshBoard();
                return;
            }

            Vector2Int source = selectedCell;
            selectedCell = new Vector2Int(-1, -1);
            StartCoroutine(TryMoveRoutine(source, clicked));
        }

        public bool TryDragMove(int row, int column, Vector2Int direction)
        {
            if (processing || gameOver || board[row, column] < 0)
            {
                return false;
            }

            int targetRow = row + direction.y;
            int targetColumn = column + direction.x;
            if (targetRow < 0
                || targetRow >= Rows
                || targetColumn < 0
                || targetColumn >= Columns)
            {
                return false;
            }

            selectedCell = new Vector2Int(-1, -1);
            StartCoroutine(TryMoveRoutine(
                new Vector2Int(column, row),
                new Vector2Int(targetColumn, targetRow)));
            return true;
        }

        private void RestartGame()
        {
            StopAllCoroutines();
            processing = false;
            gameOver = false;
            score = 0;
            moves = 0;
            highestLevel = 0;
            selectedCell = new Vector2Int(-1, -1);

            ClearBoard();
            CreateGuaranteedStartingPair();
            for (int i = 2; i < InitialTileCount; i++)
            {
                SpawnRandomTile();
            }

            RefreshBoard();
            UpdateHud("Merge two equal adjacent tiles");
        }

        private void CreateTileViews()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    RectTransform clone = Instantiate(tileTemplate, boardRoot);
                    clone.name = $"Tile_{row}_{column}";
                    clone.sizeDelta = new Vector2(TileSize, TileSize);
                    clone.gameObject.SetActive(true);

                    MergeTwoTileView view =
                        clone.gameObject.AddComponent<MergeTwoTileView>();
                    view.Initialize(this);
                    views[row, column] = view;
                }
            }
        }

        private IEnumerator TryMoveRoutine(Vector2Int source, Vector2Int target)
        {
            processing = true;
            int sourceLevel = board[source.y, source.x];
            int targetLevel = board[target.y, target.x];
            if (sourceLevel < 0)
            {
                processing = false;
                yield break;
            }

            bool merged = targetLevel == sourceLevel;
            bool moved = targetLevel < 0;
            if (!merged && !moved)
            {
                selectedCell = target;
                UpdateHud("Only equal tiles can merge");
                RefreshBoard();
                processing = false;
                yield break;
            }

            board[source.y, source.x] = -1;
            if (merged)
            {
                int newLevel = sourceLevel + 1;
                board[target.y, target.x] = newLevel;
                highestLevel = Mathf.Max(highestLevel, newLevel);
                score += GetTileValue(newLevel);
                UpdateHud($"+{GetTileValue(newLevel)} merge!");
            }
            else
            {
                board[target.y, target.x] = sourceLevel;
                UpdateHud("Tile moved");
            }

            moves++;
            RefreshBoard();
            yield return new WaitForSecondsRealtime(StepDuration);

            SpawnRandomTile();
            RefreshBoard();
            yield return new WaitForSecondsRealtime(StepDuration);

            if (!HasAvailableMove())
            {
                gameOver = true;
                UpdateHud($"Game over - highest tile {GetTileValue(highestLevel)}");
            }
            else
            {
                UpdateHud(merged
                    ? "Nice! Keep merging"
                    : "Merge two equal adjacent tiles");
            }

            processing = false;
        }

        private void ClearBoard()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    board[row, column] = -1;
                }
            }
        }

        private void CreateGuaranteedStartingPair()
        {
            int row = Random.Range(0, Rows);
            int column = Random.Range(0, Columns - 1);
            board[row, column] = 0;
            board[row, column + 1] = 0;
        }

        private bool SpawnRandomTile()
        {
            int emptyCount = 0;
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (board[row, column] < 0)
                    {
                        emptyCount++;
                    }
                }
            }

            if (emptyCount == 0)
            {
                return false;
            }

            int chosen = Random.Range(0, emptyCount);
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (board[row, column] >= 0)
                    {
                        continue;
                    }

                    if (chosen-- == 0)
                    {
                        board[row, column] = Random.value < 0.18f ? 1 : 0;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasAvailableMove()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    if (board[row, column] < 0)
                    {
                        return true;
                    }

                    if (column + 1 < Columns
                        && board[row, column] == board[row, column + 1])
                    {
                        return true;
                    }

                    if (row + 1 < Rows
                        && board[row, column] == board[row + 1, column])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearSelection()
        {
            selectedCell = new Vector2Int(-1, -1);
            UpdateHud("Merge two equal adjacent tiles");
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
                    int level = board[row, column];
                    Color color = level >= 0
                        ? tileColors[level % tileColors.Length]
                        : Color.clear;
                    bool selected =
                        selectedCell.x == column && selectedCell.y == row;

                    MergeTwoTileView view = views[row, column];
                    view.Bind(row, column, level, color, selected);
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

        private static int GetTileValue(int level)
        {
            return 1 << Mathf.Clamp(level + 1, 1, 20);
        }

        private static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x)
                   + Mathf.Abs(first.y - second.y) == 1;
        }
    }
}
