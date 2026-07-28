using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Re.Desktop.ViewModels.Agenda;

public partial class AgendaViewModel : ObservableObject
{
    private readonly Dictionary<DateTime, ObservableCollection<CalendarEvent>> _eventStore = new();
    private CalendarDay? _selectedDayForEvent;

    [ObservableProperty] private string _currentMonthYear = string.Empty;
    [ObservableProperty] private DateTime _currentDate;
    [ObservableProperty] private string _selectedTypeFilter = "All";
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private EventFormModel _formModel = new();
    [ObservableProperty] private int _monthlyEventCount;
    [ObservableProperty] private int _meetingCount;
    [ObservableProperty] private int _reminderCount;
    [ObservableProperty] private int _highPriorityCount;

    public ObservableCollection<CalendarDay> CalendarDays { get; } = [];
    public ObservableCollection<CalendarEvent> UpcomingEvents { get; } = [];
    public List<string> TypeFilters { get; } = ["All", "Meeting", "Call", "Reminder", "Task"];

    public AgendaViewModel()
    {
        CurrentDate = DateTime.Today;
        LoadCalendar();
    }

    partial void OnSelectedTypeFilterChanged(string value) => LoadCalendar();

    private void AddStoredEvent(DateTime date, CalendarEvent calendarEvent)
    {
        var key = date.Date;
        if (!_eventStore.TryGetValue(key, out var events))
            _eventStore[key] = events = [];
        events.Add(calendarEvent with { Date = key });
    }

    private void LoadCalendar()
    {
        CalendarDays.Clear();
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        CurrentMonthYear = culture.TextInfo.ToTitleCase(CurrentDate.ToString("MMMM yyyy", culture));

        var firstDay = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        var leadingDays = (int)firstDay.DayOfWeek;
        leadingDays = leadingDays == 0 ? 6 : leadingDays - 1;
        for (var i = 0; i < leadingDays; i++)
            CalendarDays.Add(new CalendarDay());

        for (var dayNumber = 1; dayNumber <= DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month); dayNumber++)
        {
            var date = new DateTime(CurrentDate.Year, CurrentDate.Month, dayNumber);
            var day = new CalendarDay { DayNumber = dayNumber.ToString(), IsCurrentMonth = true, Date = date };
            if (_eventStore.TryGetValue(date, out var events))
                foreach (var calendarEvent in events.Where(MatchesFilter))
                    day.Events.Add(calendarEvent);
            CalendarDays.Add(day);
        }

        while (CalendarDays.Count < 42)
            CalendarDays.Add(new CalendarDay());
        UpdateSummaries();
    }

    private bool MatchesFilter(CalendarEvent calendarEvent) =>
        SelectedTypeFilter == "All" || calendarEvent.Type == SelectedTypeFilter;

    private void UpdateSummaries()
    {
        var monthEvents = _eventStore
            .Where(x => x.Key.Year == CurrentDate.Year && x.Key.Month == CurrentDate.Month)
            .SelectMany(x => x.Value).ToList();
        MonthlyEventCount = monthEvents.Count;
        MeetingCount = monthEvents.Count(x => x.Type == "Meeting");
        ReminderCount = monthEvents.Count(x => x.Type == "Reminder");
        HighPriorityCount = monthEvents.Count(x => x.Priority == "High");

        UpcomingEvents.Clear();
        foreach (var item in _eventStore
                     .Where(x => x.Key >= DateTime.Today)
                     .OrderBy(x => x.Key)
                     .SelectMany(x => x.Value.OrderBy(e => e.StartTime))
                     .Take(8))
            UpcomingEvents.Add(item);
    }

    [RelayCommand] private void PreviousMonth() { CurrentDate = CurrentDate.AddMonths(-1); LoadCalendar(); }
    [RelayCommand] private void NextMonth() { CurrentDate = CurrentDate.AddMonths(1); LoadCalendar(); }
    [RelayCommand] private void GoToday() { CurrentDate = DateTime.Today; LoadCalendar(); }

    [RelayCommand]
    private void AddEvent(CalendarDay? day)
    {
        if (day is null || !day.IsCurrentMonth) return;
        _selectedDayForEvent = day;
        FormModel = CreateForm(day.Date);
        IsFormOpen = true;
    }

    [RelayCommand]
    private void AddEventFromButton()
    {
        var day = CalendarDays.FirstOrDefault(x => x.IsToday)
                  ?? CalendarDays.FirstOrDefault(x => x.IsCurrentMonth);
        AddEvent(day);
    }

    private static EventFormModel CreateForm(DateTime date) => new()
    {
        EventDate = date,
        DateStr = date.ToString("dd MMMM yyyy, dddd", CultureInfo.GetCultureInfo("tr-TR"))
    };

    [RelayCommand]
    private void SaveEvent()
    {
        if (string.IsNullOrWhiteSpace(FormModel.Title) || _selectedDayForEvent is null) return;
        if (!FormModel.IsAllDay &&
            TimeSpan.TryParse(FormModel.StartTime, out var start) &&
            TimeSpan.TryParse(FormModel.EndTime, out var end) && end <= start)
            return;

        var color = FormModel.Priority == "High" ? "#C62828" :
            FormModel.Type == "Meeting" ? "#8E1717" : "#202124";
        AddStoredEvent(FormModel.EventDate, new CalendarEvent
        {
            Title = FormModel.Title.Trim(),
            Time = FormModel.IsAllDay ? "All day" : FormModel.StartTime,
            StartTime = FormModel.StartTime,
            EndTime = FormModel.EndTime,
            Type = FormModel.Type,
            Priority = FormModel.Priority,
            Reminder = FormModel.Reminder,
            Description = FormModel.Description,
            RelatedAccount = FormModel.RelatedAccount,
            IsAllDay = FormModel.IsAllDay,
            Color = color
        });
        IsFormOpen = false;
        LoadCalendar();
    }

    [RelayCommand] private void CloseForm() => IsFormOpen = false;
}

public partial class EventFormModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private DateTime _eventDate = DateTime.Today;
    [ObservableProperty] private string _startTime = "09:00";
    [ObservableProperty] private string _endTime = "10:00";
    [ObservableProperty] private bool _isAllDay;
    [ObservableProperty] private string _type = "Meeting";
    [ObservableProperty] private string _priority = "Normal";
    [ObservableProperty] private string _reminder = "15 minutes before";
    [ObservableProperty] private string _relatedAccount = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _dateStr = string.Empty;
}

public class CalendarDay : ObservableObject
{
    public string DayNumber { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; }
    public DateTime Date { get; set; }
    public bool IsToday => IsCurrentMonth && Date.Date == DateTime.Today;
    public ObservableCollection<CalendarEvent> Events { get; } = [];
}

public record CalendarEvent
{
    public DateTime Date { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal";
    public string Reminder { get; init; } = string.Empty;
    public string RelatedAccount { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsAllDay { get; init; }
    public string Color { get; init; } = "#C62828";
}
