using System.Collections.Generic;
using System.Linq;
using Script.EntitySystem.Unit;
using Script.GridSystem;
using UnityEngine;

namespace Script.PathFinding
{
    public class AStarAlgorithm : IPathfindingAlgorithm
    {
        // Classe interna per rappresentare un nodo nella griglia di ricerca.
        private class Node
        {
            public Vector2Int position;
            public Node parent;
            public float gCost; // Costo dal nodo iniziale al nodo corrente.
            public float hCost; // Costo euristico dal nodo corrente al nodo destinazione.
            public float fCost; // gCost + hCost

            public Node(Vector2Int position)
            {
                this.position = position;
            }

            public Node(Vector2Int position, Node parent, float gCost, float hCost)
            {
                this.position = position;
                this.parent = parent;
                this.gCost = gCost;
                this.hCost = hCost;
                this.fCost = gCost + hCost;
            }
        }

        // Metodo per trovare il percorso tra due celle usando l'algoritmo A*.
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group)
        {
            // Se il punto di partenza o di arrivo non sono validi, restituisci un percorso vuoto.
            if (start.x < 0 || start.x >= GameManager.Instance.gridManager.width || start.y < 0 || start.y >= GameManager.Instance.gridManager.height ||
                end.x < 0 || end.x >= GameManager.Instance.gridManager.width || end.y < 0 || end.y >= GameManager.Instance.gridManager.height)
            {
                return new List<Vector2Int>();
            }

            // Se la cella di destinazione non è percorribile, restituisci un percorso vuoto.
            if (!GameManager.Instance.GetCell(end).IsWalkable())
            {
                return new List<Vector2Int>();
            }

            // Inizializza le liste openSet e closedSet per la ricerca del percorso.
            List<Node> openSet = new List<Node>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            // Crea il nodo iniziale e lo aggiunge all'openSet.
            Node startNode = new Node(start, null, 0, CalculateHeuristic(start, end));
            openSet.Add(startNode);

            // Ciclo principale dell'algoritmo A*.
            while (openSet.Count > 0)
            {
                // Trova il nodo con il costo F più basso nell'openSet.
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                // Rimuove il nodo corrente dall'openSet e lo aggiunge al closedSet.
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                // Se il nodo corrente è il nodo di destinazione, ricostruisce e restituisce il percorso.
                if (currentNode.position == end)
                {
                    return ReconstructPath(currentNode);
                }

                // Esplora i vicini del nodo corrente.
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        // Salta il nodo corrente.
                        if (xOffset == 0 && yOffset == 0)
                        {
                            continue;
                        }

                        // Calcola la posizione del vicino.
                        Vector2Int neighborPosition = new Vector2Int(currentNode.position.x + xOffset, currentNode.position.y + yOffset);

                        // Se il vicino non è valido o è già nel closedSet, passa al prossimo vicino.
                        if (neighborPosition.x < 0 || neighborPosition.x >= GameManager.Instance.gridManager.width || neighborPosition.y < 0 || neighborPosition.y >= GameManager.Instance.gridManager.height || closedSet.Contains(neighborPosition))
                        {
                            continue;
                        }

                        // Se il vicino non è percorribile e non c'è un'unità del gruppo sulla cella, passa al prossimo vicino.
                        Cell neighborCell = GameManager.Instance.GetCell(neighborPosition);
                        if (!neighborCell.IsWalkable() && (group == null || group.All(u => u.GetPosition() != neighborPosition)))
                        {
                            continue;
                        }

                        // Calcola il costo G del vicino.
                        float newGCost = currentNode.gCost + (xOffset == 0 || yOffset == 0 ? 1 : 1.414f); // Costo diagonale: circa 1.414

                        // Se il vicino non è nell'openSet o ha un costo G più alto, aggiorna le informazioni del vicino.
                        if (!openSet.Any(node => node.position == neighborPosition) || newGCost < openSet.First(node => node.position == neighborPosition).gCost)
                        {
                            float hCost = CalculateHeuristic(neighborPosition, end);
                            Node neighborNode = new Node(neighborPosition, currentNode, newGCost, hCost);
                            if (!openSet.Any(node => node.position == neighborPosition))
                            {
                                openSet.Add(neighborNode);
                            }
                            else
                            {
                                openSet.Remove(openSet.First(node => node.position == neighborPosition));
                                openSet.Add(neighborNode);
                            }
                        }
                    }
                }
            }

            // Se non è stato trovato alcun percorso, restituisce un percorso vuoto.
            return new List<Vector2Int>();
        }

        // Metodo per calcolare l'euristica (distanza Manhattan) tra due celle.
        private float CalculateHeuristic(Vector2Int start, Vector2Int end)
        {
            return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
            //return Vector2.Distance(new Vector2(start.x, start.y), new Vector2(end.x, end.y)); //alternativa
        }

        // Metodo per ricostruire il percorso dal nodo di destinazione al nodo iniziale.
        private List<Vector2Int> ReconstructPath(Node node)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            while (node != null)
            {
                path.Add(node.position);
                node = node.parent;
            }
            path.Reverse(); // Inverte il percorso per ottenere l'ordine corretto (dal nodo iniziale al nodo destinazione).
            return path;
        }
    }
}