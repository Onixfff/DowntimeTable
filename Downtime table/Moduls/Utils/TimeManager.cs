using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downtime_table.Moduls.Utils
{
    public class TimeManager
    {
        // Константы для временных интервалов
        private static readonly TimeSpan MORNING_SHIFT_START = new TimeSpan(7, 30, 0);
        private static readonly TimeSpan MORNING_SHIFT_END = new TimeSpan(20, 29, 0);
        private static readonly TimeSpan EVENING_SHIFT_START = new TimeSpan(20, 0, 0);
        private static readonly TimeSpan EVENING_SHIFT_END = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan DAY_START = new TimeSpan(0, 0, 0);
        private static readonly TimeSpan DAY_END = new TimeSpan(23, 59, 59);

        /// <summary>
        /// Определяет тип смены на основе текущего времени
        /// </summary>
        public enum ShiftType
        {
            Morning,    // Утренняя смена (07:30 - 20:29)
            EveningStart, // Начало вечерней смены (20:00 - 23:59)
            EveningEnd,   // Конец вечерней смены (00:00 - 08:29)
            Unknown     // Неизвестный интервал
        }

        /// <summary>
        /// Определяет тип смены на основе времени
        /// </summary>
        /// <param name="time">Время для определения типа смены</param>
        /// <returns>Тип смены</returns>
        public ShiftType DetermineShiftType(DateTime time)
        {
            TimeSpan timeOfDay = time.TimeOfDay;

            if (timeOfDay >= MORNING_SHIFT_START && timeOfDay < MORNING_SHIFT_END)
            {
                return ShiftType.Morning;
            }
            else if (timeOfDay >= EVENING_SHIFT_START && timeOfDay <= DAY_END)
            {
                return ShiftType.EveningStart;
            }
            else if (timeOfDay >= DAY_START && timeOfDay <= EVENING_SHIFT_END)
            {
                return ShiftType.EveningEnd;
            }

            return ShiftType.Unknown;
        }

        /// <summary>
        /// Получает временной интервал для запроса на основе типа смены
        /// </summary>
        /// <param name="currentTime">Текущее время</param>
        /// <returns>Кортеж с начальной и конечной датой, а также строками времени начала и конца</returns>
        public (DateTime startDate, DateTime endDate, string startTime, string endTime) GetTimeInterval(DateTime currentTime)
        {
            ShiftType shiftType = DetermineShiftType(currentTime);

            switch (shiftType)
            {
                case ShiftType.Morning:
                    return (currentTime, currentTime, "07:30:00", "20:29:00");

                case ShiftType.EveningStart:
                    return (currentTime, currentTime.AddDays(1), "20:00:00", "08:00:00");

                case ShiftType.EveningEnd:
                    return (currentTime.AddDays(-1), currentTime, "20:00:00", "08:00:00");

                default:
                    throw new ArgumentException("Неизвестный временной интервал", nameof(currentTime));
            }
        }
    }
}
