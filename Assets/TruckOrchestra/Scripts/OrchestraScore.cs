using System;
using LWS.TruckDanceOff;
using UnityEngine;

namespace LWS.TruckOrchestra
{
    [CreateAssetMenu(menuName = "Truck Sports/Orchestra arrangement parts")]
    public sealed class OrchestraScore : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public string instrument;
            public DanceSongLibrary.Chart[] difficulties = new DanceSongLibrary.Chart[4];
        }
        [Serializable] public sealed class Arrangement { public Part[] parts = new Part[4]; }
        public Arrangement[] arrangements = new Arrangement[7];
        public DanceNote[] Chart(int song, int participant, int difficulty)
        {
            if (song < 0 || song >= arrangements.Length || participant < 0 || participant >= 4 || difficulty < 0 || difficulty >= 4)
                throw new ArgumentOutOfRangeException("Original seven arrangements, four owned parts and four chart difficulties are required.");
            return arrangements[song].parts[participant].difficulties[difficulty].notes;
        }
        public void Validate(DanceSongLibrary library)
        {
            if (library == null || arrangements == null || arrangements.Length != 7)
                throw new InvalidOperationException("Assign all seven original Orchestra arrangements.");
            library.Validate();
            for (int song = 0; song < 7; song++)
            {
                if (arrangements[song] == null || arrangements[song].parts == null || arrangements[song].parts.Length != 4)
                    throw new InvalidOperationException("Every arrangement needs four independent instrumental parts.");
                for (int slot = 0; slot < 4; slot++) for (int difficulty = 0; difficulty < 4; difficulty++)
                {
                    var notes = Chart(song, slot, difficulty);
                    if (notes == null || notes.Length == 0) throw new InvalidOperationException("An instrumental part cannot be empty.");
                    new DanceRules(notes);
                    foreach (var note in notes)
                        if (note.time + note.duration >= library.Get(song).duration)
                            throw new InvalidOperationException("Instrumental notes must finish inside the actual audio performance.");
                }
            }
        }
        public static int MidiFor(int part, int lane, int bar, int key)
        {
            int[] roots = { 0, 5, 9, 7 }; int[] intervals = { 0, 4, 7, 12 };
            return 48 + key + roots[(bar % 4 + 4) % 4] + intervals[lane] + (part == 0 || part == 3 ? 12 : 0);
        }
    }
}
