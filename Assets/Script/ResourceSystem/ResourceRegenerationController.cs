using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Gestisce la coda di rigenerazione delle risorse senza conoscere la logica di complete/regeneration finale.
    /// Mantiene il tracking dei timer e restituisce gli elementi completati al chiamante.
    /// </summary>
    public sealed class ResourceRegenerationController
    {
        private readonly List<ResourceRegenerationJob> _activeRegenerations = new();

        public int Count => _activeRegenerations.Count;

        public bool HasPendingAt(Vector2Int position)
        {
            for (int i = 0; i < _activeRegenerations.Count; i++)
            {
                if (_activeRegenerations[i].Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(Vector2Int position, ResourceDataSO data, float timeLeft, GameObject visualObject)
        {
            _activeRegenerations.Add(new ResourceRegenerationJob(position, data, timeLeft, visualObject));
        }

        public void Tick(float deltaTime, List<ResourceRegenerationJob> completedRegenerations)
        {
            if (_activeRegenerations.Count == 0)
            {
                return;
            }

            for (int i = _activeRegenerations.Count - 1; i >= 0; i--)
            {
                var regen = _activeRegenerations[i];
                float timeLeft = regen.TimeLeft - deltaTime;

                if (timeLeft <= 0f)
                {
                    completedRegenerations.Add(regen);
                    _activeRegenerations.RemoveAt(i);
                }
                else
                {
                    _activeRegenerations[i] = regen.WithTimeLeft(timeLeft);
                }
            }
        }

        public void Clear()
        {
            _activeRegenerations.Clear();
        }
    }
}
