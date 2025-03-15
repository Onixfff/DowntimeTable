namespace Downtime_table.Moduls.Data.Sources
{
    public class ViewDate : BaseDate
    {
        // Дополнительные свойства для UI
        public bool IsModified { get; set; } = false;
        public bool IsValid => Validate();

        // Конструктор из ProcessedDate
        public ViewDate(ProcessedDate data)
            : base(data)
        {
        }

        // Конструктор из ArchivedDate
        public ViewDate(ArchivedDate data)
            : base(data)
        {
        }

        // Метод валидации
        private bool Validate()
        {
            // Пример валидации
            if (IdTypeDowntime == null || string.IsNullOrEmpty(Comment))
                return false;

            return true;
        }

        // Метод для отслеживания изменений
        public void MarkAsModified()
        {
            IsModified = true;
        }
    }
}
