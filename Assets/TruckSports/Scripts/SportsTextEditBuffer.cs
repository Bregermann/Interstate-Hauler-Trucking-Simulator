using System;
using System.Globalization;
namespace LWS.TruckSports
{
    // Detached text only: persistence and the target input field are touched on commit.
    public sealed class SportsTextEditBuffer
    {
        public string Original { get; }
        public string Text { get; private set; }
        public int Limit { get; }
        public bool Closed { get; private set; }
        public SportsTextEditBuffer(string original, int limit)
        { Original = original ?? ""; Text = Original; Limit = Math.Max(0, limit); }
        public bool Insert(string value)
        {
            if (Closed || string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i])) return false;
                if (char.IsHighSurrogate(value[i])) { if (++i >= value.Length || !char.IsLowSurrogate(value[i])) return false; }
                else if (char.IsLowSurrogate(value[i])) return false;
            }
            if (Limit > 0 && Text.Length + value.Length > Limit) return false;
            Text += value; return true;
        }
        public bool ReplaceDraft(string value)
        {
            if (Closed || value == null) return false;
            var next = new SportsTextEditBuffer("", Limit);
            if (value.Length > 0 && !next.Insert(value)) return false;
            Text = value; return true;
        }
        public bool Clear() { if (Closed) return false; Text = ""; return true; }
        public bool Backspace()
        {
            if (Closed || Text.Length == 0) return false;
            int[] elements = StringInfo.ParseCombiningCharacters(Text);
            Text = Text.Substring(0, elements[elements.Length - 1]); return true;
        }
        public string Commit() { if (Closed) throw new InvalidOperationException("This text edit is closed."); Closed = true; return Text; }
        public string Cancel() { if (Closed) throw new InvalidOperationException("This text edit is closed."); Closed = true; Text = Original; return Original; }
    }
}
