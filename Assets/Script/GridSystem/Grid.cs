using System.Collections.Generic;
using UnityEngine;

namespace Script.GridSystem
{
    /// <summary>
    /// Classe generica che modella una griglia 2D (isometrica tramite Math3x4).
    /// Contiene i dati e gestisce conversioni Grid <-> World.
    /// </summary>
    public class Grid<T>
    {
        #region Private Fields
        
        private int _width;
        private int _height;
        private float _cellSize;

        private Vector3 _originPosition;

        private T[,] _gridT;

        private Matrix4x4 _matrix;
        
        #endregion

        #region Initialization

        /// <summary>
        /// Crea una griglia vuota specificando dimensioni e taglia delle celle.
        /// </summary>
        public Grid(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            this._gridT = new T[width, height];

            _originPosition = new Vector3(0, 0, 1);

            InitMatrix4x4();
        }

        private void InitMatrix4x4()
        {
            _matrix = Matrix4x4.identity;
            _matrix.SetColumn(0, new Vector4(1f, 0.5f, 0f, 0f)); // Colonna per x
            _matrix.SetColumn(1, new Vector4(-1f, 0.5f, 0f, 0f)); // Colonna per y
            _matrix.SetColumn(2, new Vector4(0f, 0f, 1f, 0f)); // Colonna per z  
        }
        
        #endregion

        #region Debug
        private void ShowDebugLines()
        {
            for (int x = 0; x < _gridT.GetLength(0); x++)
            {
                for (int y = 0; y < _gridT.GetLength(1); y++)
                {
                    Vector3 worldPos = GetIsoToWorldPosition(x, y);
                    Vector3 worldPosX = GetIsoToWorldPosition(x + 1, y);
                    Vector3 worldPosY = GetIsoToWorldPosition(x, y + 1);

                    // piccolo offset Z per rendere le linee visibili sopra i tile
                    worldPos.z -= 0.1f;
                    worldPosX.z -= 0.1f;
                    worldPosY.z -= 0.1f;

                    Debug.DrawLine(worldPos, worldPosX, Color.black, 100f);
                    Debug.DrawLine(worldPos, worldPosY, Color.black, 100f);
                }
            }

            // Disegna le linee di bordo
            Vector3 topLeft = GetIsoToWorldPosition(0, _height);
            Vector3 topRight = GetIsoToWorldPosition(_width, _height);
            Vector3 bottomRight = GetIsoToWorldPosition(_width, 0);

            topLeft.z -= 0.1f;
            topRight.z -= 0.1f;
            bottomRight.z -= 0.1f;

            Debug.DrawLine(topLeft, topRight, Color.black, 100f);
            Debug.DrawLine(topRight, bottomRight, Color.black, 100f);
        }
        #endregion Debug

        #region Public APIs

        /// <summary>
        /// Ottiene la posizione nel mondo (con proiezione isometrica) dalle coordinate logiche.
        /// </summary>
        public Vector3 GetIsoToWorldPosition(int x, int y)
        {
            Vector3 cartesianPos = new Vector3(x, y) * _cellSize;
            Vector3 isoPos = _matrix.MultiplyPoint3x4(cartesianPos);
            return new Vector3(isoPos.x, isoPos.y, 0) + _originPosition;
        }

        /// <summary>
        /// Converte la posizione nel mondo in coordinate logiche di griglia.
        /// Usa RoundToInt per favorire lo snap al centro del diamante.
        /// </summary>
        public void GetWorldToIsoPosition(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 localPos = worldPosition - _originPosition;

            Vector3 cartesianPos = _matrix.inverse.MultiplyPoint3x4(localPos);
            //x = Mathf.FloorToInt(cartesianPos.x / _cellSize);
            //y = Mathf.FloorToInt(cartesianPos.y / _cellSize);

            // Usa Round invece di Floor per uno snap al centro della cella (diamante isometrico)
            // Così il snap avviene al 50% del diamante, non al suo bordo inferiore
            x = Mathf.RoundToInt(cartesianPos.x / _cellSize);
            y = Mathf.RoundToInt(cartesianPos.y / _cellSize);
        }

        /// <summary>
        /// Imposta il valore di una cella specificando le coordinate logiche giuste.
        /// </summary>
        public void SetValue(int x, int y, T value)
        {
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                _gridT[x, y] = value;
            }
        }

        /// <summary>
        /// Imposta il valore di una cella in base alla posizione reale nel mondo.
        /// </summary>
        public void SetValue(Vector3 worldPosition, T value)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            SetValue(x, y, value);
        }

        /// <summary>
        /// Recupera il valore archiviato in una determinata cella logica.
        /// </summary>
        public T GetValue(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return _gridT[x, y];
            }

            return default(T);
        }

        /// <summary>
        /// Recupera il valore in griglia passando la posizione mondo.
        /// </summary>
        public T GetValue(Vector3 worldPosition)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            return GetValue(x, y);
        }

        /// <summary>
        /// Snappa una posizione arbitraria al centro della cella isometrica piu' vicina.
        /// </summary>
        public Vector3 SnapWorldToGridWorld(Vector3 worldPosition)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            return GetIsoToWorldPosition(x, y);
        }

        /// <summary>
        /// Converte la griglia 2D in una flat List (usato da Pathfinding Tester e similari).
        /// </summary>
        public List<T> ToList()
        {
            List<T> listT = new();
            foreach (var tile in _gridT)
            {
                listT.Add(tile);
            }

            return listT;
        }

        #endregion

    }
}
