using System;
using UnityEngine;
namespace LWS.TruckDanceOff {
[CreateAssetMenu(menuName="Truck Sports/Dance song library")]
public sealed class DanceSongLibrary:ScriptableObject {
 [Serializable]public sealed class Chart {public DanceNote[] notes;}
 [Serializable]public sealed class Song {public string title,genre;public AudioClip music;public float bpm=120;public double duration,freestyleStart,freestyleEnd;public Chart[] charts=new Chart[4];public double Beat=>60.0/bpm;}
 public Song[] songs;
 public Song Get(int index){if(songs==null||songs.Length==0)throw new InvalidOperationException("Assign the original Dance-Off song library.");return songs[Mathf.Clamp(index,0,songs.Length-1)];}
 public void Validate(){if(songs==null||songs.Length<7)throw new InvalidOperationException("Dance-Off requires the seven original genre tracks.");foreach(var s in songs){if(s.music==null||s.charts==null||s.charts.Length!=4||s.bpm<=0||Math.Abs(s.music.length-s.duration)>.05)throw new InvalidOperationException("Music/chart duration mismatch: "+s.title);foreach(var c in s.charts){if(c==null||c.notes==null||c.notes.Length==0)throw new InvalidOperationException("Missing chart: "+s.title);new DanceRules(c.notes);}}}
}
}
