using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms.VisualStyles;

public class DowntimeAnalyzer
{
    public Dictionary<string, TimeSpan> DowntimeSummary { get; private set; }
    public TimeSpan TotalDowntime { get; private set; }

    public DowntimeAnalyzer()
    {
        DowntimeSummary = new Dictionary<string, TimeSpan>();
    }

    public void ClearData()
    {
        DowntimeSummary.Clear();
        TotalDowntime = TimeSpan.Zero;
    }

    public void AnalyzeDowntime(DataTable dataTable)
    {
        foreach (DataRow row in dataTable.Rows)
        {
            string downtimeType = row["Вид простоя"].ToString();
            TimeSpan downtimeDuration = (TimeSpan)row["Время простоя"];

            if (DowntimeSummary.ContainsKey(downtimeType))
            {
                DowntimeSummary[downtimeType] += downtimeDuration;
            }
            else
            {
                DowntimeSummary.Add(downtimeType, downtimeDuration);
            }
        }

        TotalDowntime = new TimeSpan(DowntimeSummary.Values.Sum(d => d.Ticks));
    }

    public void PrintDowntimeDetails()
    {
        foreach (var entry in DowntimeSummary)
        {
            double downtimePercentage = (entry.Value.TotalSeconds / TotalDowntime.TotalSeconds) * 100;
            Console.WriteLine($"Вид простоя: {entry.Key}, Время простоя: {entry.Value}, % Простоя: {downtimePercentage:F2}");
        }
    }

    public TimeSpan GetTotalTime()
    {
        TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0);
        TimeSpan total = new TimeSpan();
        foreach (var item in DowntimeSummary)
        {
            timeSpan = total;
            total = timeSpan.Add(item.Value);
        }

        return timeSpan;
    }

    public int GetNumberTypesDowntime()
    {
        return DowntimeSummary.Count;
    }

    public string GetNameTypeDowntime(int id)
    {
        string name = string.Empty;

        List<string> keys = new List<string>(DowntimeSummary.Keys);
        
        for(int i = 0; i < keys.Count; i++)
        {
            if(id == i)
            {
                string value = keys[i];
                name = value;
            }
        }
        return name;
    }

    public TimeSpan GetCountTypeDowntime(int id)
    {
        TimeSpan name = TimeSpan.Zero;

        List<TimeSpan> keys = new List<TimeSpan>(DowntimeSummary.Values);

        for (int i = 0; i < keys.Count; i++)
        {
            if (id == i)
            {
                TimeSpan value = keys[i];
                name = value;
            }
        }

        return name;
    }
}
