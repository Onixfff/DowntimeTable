namespace Downtime_table
{
    public class DateIdle
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public DateIdle(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
