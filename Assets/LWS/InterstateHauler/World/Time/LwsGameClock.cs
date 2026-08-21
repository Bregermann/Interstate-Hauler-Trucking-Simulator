using System;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsGameDateTime
    {
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public float second;

        public LwsGameDateTime(int year, int month, int day, int hour, int minute, float second)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        public float TimeOfDayHours => LwsGameClockUtility.NormalizeHours(hour + minute / 60f + second / 3600f);

        public DateTime ToDateTime()
        {
            int safeYear = Mathf.Clamp(year, 1, 9999);
            int safeMonth = Mathf.Clamp(month, 1, 12);
            int safeDay = Mathf.Clamp(day, 1, DateTime.DaysInMonth(safeYear, safeMonth));
            int safeHour = Mathf.Clamp(hour, 0, 23);
            int safeMinute = Mathf.Clamp(minute, 0, 59);
            int safeSecond = Mathf.Clamp(Mathf.FloorToInt(second), 0, 59);
            int safeMillisecond = Mathf.Clamp(Mathf.RoundToInt((second - safeSecond) * 1000f), 0, 999);
            return new DateTime(safeYear, safeMonth, safeDay, safeHour, safeMinute, safeSecond, safeMillisecond, DateTimeKind.Unspecified);
        }

        public static LwsGameDateTime FromDateTime(DateTime dateTime)
        {
            return new LwsGameDateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second + dateTime.Millisecond / 1000f);
        }
    }

    [Serializable]
    public sealed class LwsGameClockTuning
    {
        public int startYear = 2026;
        public int startMonth = 6;
        public int startDay = 1;
        [Range(0f, 23.999f)] public float startTimeOfDayHours = 8f;
        public float defaultTimeScale = 1f;
        public float daylightStartHours = 6f;
        public float daylightEndHours = 20f;
        public float[] developmentTimeScales = { 1f, 6f, 20f, 60f };

        public LwsGameDateTime StartDateTime => LwsGameClockUtility.CreateDateTime(startYear, startMonth, startDay, startTimeOfDayHours);

        public bool Validate(out string message)
        {
            if (startYear < 1 || startYear > 9999 || startMonth < 1 || startMonth > 12)
            {
                message = "Game clock start date is outside the supported DateTime range.";
                return false;
            }

            if (startDay < 1 || startDay > DateTime.DaysInMonth(startYear, startMonth))
            {
                message = "Game clock start day is invalid for the configured month.";
                return false;
            }

            if (defaultTimeScale < 0f)
            {
                message = "Game clock default time scale cannot be negative.";
                return false;
            }

            if (Mathf.Approximately(daylightStartHours, daylightEndHours))
            {
                message = "Game clock daylight start and end cannot be identical.";
                return false;
            }

            message = "Game clock tuning is valid.";
            return true;
        }

        public static LwsGameClockTuning CreateValidationDefault()
        {
            return new LwsGameClockTuning
            {
                startYear = 2026,
                startMonth = 6,
                startDay = 1,
                startTimeOfDayHours = 8f,
                defaultTimeScale = 1f,
                daylightStartHours = 6f,
                daylightEndHours = 20f,
                developmentTimeScales = new[] { 1f, 6f, 20f, 60f }
            };
        }
    }

    [Serializable]
    public struct LwsGameClockSnapshot
    {
        public int year;
        public int month;
        public int day;
        public DayOfWeek dayOfWeek;
        public int hour;
        public int minute;
        public float second;
        public float timeOfDayHours;
        public bool isDay;
        public bool isNight;
        public float timeScale;
        public bool paused;
        public double totalGameSeconds;
        public long versionTicks;

        public string ClockText => LwsGameClockUtility.FormatClock(this);
        public string DateText => $"{year:0000}-{month:00}-{day:00}";
    }

    public interface ILwsGameClockService : ILwsService
    {
        LwsGameClockSnapshot CurrentSnapshot { get; }
        LwsGameClockTuning ActiveTuning { get; }
        string LastMessage { get; }
        event Action<LwsGameClockSnapshot> ClockChanged;
        void Configure(LwsGameClockTuning tuning);
        void SetPaused(bool paused);
        void SetTimeScale(float timeScale);
        void SetTimeOfDayHours(float hours);
        void AddHours(float hours);
        void SetDateTime(LwsGameDateTime dateTime);
        void Tick(float realDeltaSeconds);
    }

    public sealed class LwsGameClockService : ILwsGameClockService
    {
        private DateTime _dateTime;
        private double _totalGameSeconds;
        private long _versionTicks;
        private bool _paused;

        public string ServiceId => "lws.game.clock";
        public LwsGameClockSnapshot CurrentSnapshot { get; private set; }
        public LwsGameClockTuning ActiveTuning { get; private set; } = LwsGameClockTuning.CreateValidationDefault();
        public string LastMessage { get; private set; } = "Not initialized.";

        public event Action<LwsGameClockSnapshot> ClockChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            Configure(ActiveTuning);
            return LwsServiceResult.Success("LWS game clock initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            LastMessage = "LWS game clock shut down.";
            ClockChanged = null;
            return LwsServiceResult.Success(LastMessage);
        }

        public void Configure(LwsGameClockTuning tuning)
        {
            ActiveTuning = tuning ?? LwsGameClockTuning.CreateValidationDefault();
            if (!ActiveTuning.Validate(out string message))
            {
                LastMessage = message;
                ActiveTuning = LwsGameClockTuning.CreateValidationDefault();
            }

            _dateTime = ActiveTuning.StartDateTime.ToDateTime();
            _totalGameSeconds = 0d;
            _paused = false;
            SetTimeScale(ActiveTuning.defaultTimeScale);
            LastMessage = "LWS game clock configured.";
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused)
            {
                return;
            }

            _paused = paused;
            Publish("Game clock pause state changed.");
        }

        public void SetTimeScale(float timeScale)
        {
            float safeScale = Mathf.Max(0f, timeScale);
            LwsGameClockSnapshot snapshot = CurrentSnapshot;
            if (Mathf.Approximately(snapshot.timeScale, safeScale) && _versionTicks > 0)
            {
                return;
            }

            CurrentSnapshot = snapshot;
            Publish("Game clock time scale changed.", safeScale);
        }

        public void SetTimeOfDayHours(float hours)
        {
            LwsGameDateTime current = LwsGameDateTime.FromDateTime(_dateTime);
            LwsGameDateTime replacement = LwsGameClockUtility.CreateDateTime(current.year, current.month, current.day, hours);
            _dateTime = replacement.ToDateTime();
            Publish($"Game clock time set to {replacement.TimeOfDayHours:0.00}h.");
        }

        public void AddHours(float hours)
        {
            if (Mathf.Approximately(hours, 0f))
            {
                return;
            }

            _dateTime = _dateTime.AddHours(hours);
            _totalGameSeconds += hours * 3600d;
            Publish($"Game clock adjusted by {hours:0.##}h.");
        }

        public void SetDateTime(LwsGameDateTime dateTime)
        {
            _dateTime = dateTime.ToDateTime();
            Publish("Game clock date/time set.");
        }

        public void Tick(float realDeltaSeconds)
        {
            if (_paused || realDeltaSeconds <= 0f || CurrentSnapshot.timeScale <= 0f)
            {
                return;
            }

            double gameSeconds = Math.Max(0d, realDeltaSeconds) * CurrentSnapshot.timeScale;
            _dateTime = _dateTime.AddSeconds(gameSeconds);
            _totalGameSeconds += gameSeconds;
            Publish("Game clock advanced.");
        }

        private void Publish(string message)
        {
            Publish(message, CurrentSnapshot.timeScale);
        }

        private void Publish(string message, float timeScale)
        {
            _versionTicks++;
            CurrentSnapshot = LwsGameClockUtility.CreateSnapshot(_dateTime, ActiveTuning, timeScale, _paused, _totalGameSeconds, _versionTicks);
            LastMessage = message ?? string.Empty;
            ClockChanged?.Invoke(CurrentSnapshot);
        }
    }

    public static class LwsGameClockUtility
    {
        public static float NormalizeHours(float hours)
        {
            if (float.IsNaN(hours) || float.IsInfinity(hours))
            {
                return 8f;
            }

            hours %= 24f;
            return hours < 0f ? hours + 24f : hours;
        }

        public static LwsGameDateTime CreateDateTime(int year, int month, int day, float timeOfDayHours)
        {
            float normalized = NormalizeHours(timeOfDayHours);
            int hour = Mathf.FloorToInt(normalized);
            float minuteFloat = (normalized - hour) * 60f;
            int minute = Mathf.FloorToInt(minuteFloat);
            float second = (minuteFloat - minute) * 60f;
            return new LwsGameDateTime(year, month, day, hour, minute, second);
        }

        public static LwsGameClockSnapshot CreateSnapshot(
            DateTime dateTime,
            LwsGameClockTuning tuning,
            float timeScale,
            bool paused,
            double totalGameSeconds,
            long versionTicks)
        {
            float timeOfDayHours = dateTime.Hour + dateTime.Minute / 60f + (dateTime.Second + dateTime.Millisecond / 1000f) / 3600f;
            bool isDay = IsDay(timeOfDayHours, tuning);
            return new LwsGameClockSnapshot
            {
                year = dateTime.Year,
                month = dateTime.Month,
                day = dateTime.Day,
                dayOfWeek = dateTime.DayOfWeek,
                hour = dateTime.Hour,
                minute = dateTime.Minute,
                second = dateTime.Second + dateTime.Millisecond / 1000f,
                timeOfDayHours = NormalizeHours(timeOfDayHours),
                isDay = isDay,
                isNight = !isDay,
                timeScale = Mathf.Max(0f, timeScale),
                paused = paused,
                totalGameSeconds = totalGameSeconds,
                versionTicks = versionTicks
            };
        }

        public static bool IsDay(float timeOfDayHours, LwsGameClockTuning tuning)
        {
            tuning ??= LwsGameClockTuning.CreateValidationDefault();
            float hour = NormalizeHours(timeOfDayHours);
            float start = NormalizeHours(tuning.daylightStartHours);
            float end = NormalizeHours(tuning.daylightEndHours);
            if (start < end)
            {
                return hour >= start && hour < end;
            }

            return hour >= start || hour < end;
        }

        public static string FormatClock(LwsGameClockSnapshot snapshot)
        {
            int hour = Mathf.Clamp(snapshot.hour, 0, 23);
            string suffix = hour >= 12 ? "PM" : "AM";
            int hour12 = hour % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            return $"{snapshot.dayOfWeek.ToString().Substring(0, 3).ToUpperInvariant()} {hour12}:{snapshot.minute:00} {suffix}";
        }
    }

    [DefaultExecutionOrder(90)]
    [DisallowMultipleComponent]
    public sealed class LwsGameClockCoordinator : MonoBehaviour
    {
        [SerializeField] private bool configureOnStart;
        [SerializeField] private bool tickClock = true;
        [SerializeField] private bool useUnscaledDeltaTime = true;
        [SerializeField] private LwsGameClockTuning tuning = LwsGameClockTuning.CreateValidationDefault();

        private ILwsGameClockService _clockService;

        public static LwsGameClockCoordinator ActiveInstance { get; private set; }
        public bool TickClock => tickClock;

        private void OnEnable()
        {
            if (ActiveInstance == null)
            {
                ActiveInstance = this;
            }
        }

        private void Start()
        {
            ResolveService();
            if (configureOnStart)
            {
                _clockService?.Configure(tuning);
            }
        }

        private void Update()
        {
            ResolveService();
            if (!tickClock)
            {
                return;
            }

            _clockService?.Tick(useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        private void OnDisable()
        {
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }

        private void ResolveService()
        {
            if (_clockService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _clockService);
        }
    }

    [DefaultExecutionOrder(720)]
    [DisallowMultipleComponent]
    public sealed class LwsGameClockHud : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-24f, -24f);

        private ILwsGameClockService _clockService;
        private Canvas _canvas;
        private Text _clockText;
        private long _lastVersion = long.MinValue;

        private void Start()
        {
            EnsureHud();
        }

        private void Update()
        {
            ResolveService();
            if (_clockText == null)
            {
                EnsureHud();
            }

            if (_clockText == null || _clockService == null)
            {
                return;
            }

            LwsGameClockSnapshot snapshot = _clockService.CurrentSnapshot;
            if (_lastVersion == snapshot.versionTicks)
            {
                return;
            }

            _lastVersion = snapshot.versionTicks;
            _clockText.enabled = visible;
            _clockText.text = snapshot.ClockText;
        }

        private void EnsureHud()
        {
            if (_canvas != null && _clockText != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("IH Game Clock HUD", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 6200;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var textObject = new GameObject("Clock Text", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(190f, 30f);
            rect.anchoredPosition = anchoredPosition;

            _clockText = textObject.AddComponent<Text>();
            try
            {
                _clockText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                                  Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                _clockText.font = null;
            }

            _clockText.fontSize = 17;
            _clockText.fontStyle = FontStyle.Bold;
            _clockText.alignment = TextAnchor.MiddleRight;
            _clockText.color = new Color(0.9f, 0.96f, 0.94f, 0.9f);
            _clockText.raycastTarget = false;
            _clockText.text = "--:--";
        }

        private void ResolveService()
        {
            if (_clockService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _clockService);
        }
    }
}
