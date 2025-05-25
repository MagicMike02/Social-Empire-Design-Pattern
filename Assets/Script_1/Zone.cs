namespace Script
{
    public class Zone : IZone
    {
        private int id;
        private string name;

        public Zone(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public int GetId() { return id; }
        public string GetName() { return name; }
        public void SetName(string name) { this.name = name; }
    }
}