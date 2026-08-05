using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// DTO pubblico per rappresentare una rigenerazione in corso o completata.
    /// Evita che ResourceManager dipenda dal tipo interno del controller.
    /// </summary>
    public readonly struct ResourceRegenerationJob
    {
        public Vector2Int Position { get; }
        public ResourceDataSO Data { get; }
        public float TimeLeft { get; }
        public GameObject VisualObject { get; }

        public ResourceRegenerationJob(Vector2Int position, ResourceDataSO data, float timeLeft, GameObject visualObject)
        {
            Position = position;
            Data = data;
            TimeLeft = timeLeft;
            VisualObject = visualObject;
        }

        public ResourceRegenerationJob WithTimeLeft(float timeLeft)
        {
            return new ResourceRegenerationJob(Position, Data, timeLeft, VisualObject);
        }
    }
}
