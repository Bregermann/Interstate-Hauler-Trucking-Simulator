"""Original short-form arcade compositions and sample-aligned rhythm charts.

All melodies, arrangements and synthesized instrument patches in this file are
original project work. No sampled recordings, third-party songs or model outputs.
Each song is a complete 32-bar arrangement, rather than an unsynchronised loop.
"""
from pathlib import Path
import json
import math
import numpy as np
import wave

RATE = 44100
OUT = Path(__file__).parent / "Music"
OUT.mkdir(exist_ok=True)
SONGS = [
    ("Axle Acid", "Electronic", 128, [45, 41, 48, 43], [0, 3, 7, 10, 12], 0),
    ("Diesel Disco", "Disco", 120, [45, 41, 43, 40], [0, 4, 7, 9, 12], 1),
    ("Fender Funk", "Funk", 112, [40, 45, 40, 47], [0, 3, 5, 7, 10], 2),
    ("Cabside Cypher", "Hip-hop", 96, [38, 34, 41, 36], [0, 3, 5, 7, 10], 3),
    ("Honky Tonk Haul", "Country", 132, [38, 43, 45, 38], [0, 4, 7, 9, 12], 4),
    ("Overdrive Anthem", "Rock", 126, [40, 43, 38, 45], [0, 3, 5, 7, 10], 5),
    ("Chrome Mosh", "Metal", 144, [35, 38, 33, 40], [0, 2, 3, 7, 10], 6),
]
HOOKS = [
    [0, 2, 3, 1, 4, 2, 1, 0], [2, 1, 0, 2, 3, 4, 2, 1],
    [0, 0, 2, 1, 3, 2, 4, 2], [0, 2, 1, 0, 3, 1, 2, 0],
    [0, 1, 2, 4, 3, 2, 1, 0], [0, 2, 2, 3, 4, 2, 1, 0],
    [0, 1, 0, 2, 3, 2, 1, 0],
]


def instrument(midi, seconds, style, rng):
    t = np.arange(max(1, int(seconds * RATE)), dtype=np.float64) / RATE
    f = 440 * 2 ** ((midi - 69) / 12)
    attack = np.minimum(1, t / .008)
    release = np.minimum(1, (seconds - t) / .045).clip(0, 1)
    if style == "bass":
        wave = np.sin(2 * np.pi * f * t) + .22 * np.sin(4 * np.pi * f * t) + .08 * np.sin(6 * np.pi * f * t)
        envelope = attack * np.exp(-t * 3) * release
    elif style == "pad":
        wave = np.sin(2 * np.pi * f * t) + .28 * np.sin(2 * np.pi * f * 1.003 * t) + .12 * np.sin(4 * np.pi * f * t)
        envelope = np.minimum(1, t / .12) * np.exp(-t * .8) * release
    else:
        harmonics = 12 if style in ("guitar", "metal", "twang") else 5
        wave = np.zeros_like(t)
        for h in range(1, harmonics + 1):
            decay = np.exp(-t * (2.5 + h * (1.8 if style == "twang" else .7)))
            wave += np.sin(2 * np.pi * f * h * t) * decay / (h ** 1.25)
        if style in ("guitar", "metal"):
            wave = np.tanh(wave * (3.2 if style == "metal" else 1.7)) * .65
        envelope = attack * release
    return (wave * envelope).astype(np.float32)


def drum(kind, rng):
    seconds = .28 if kind == "kick" else .22 if kind == "snare" else .065
    t = np.arange(int(seconds * RATE), dtype=np.float64) / RATE
    if kind == "kick":
        phase = 2 * np.pi * (43 * t + 115 * .022 * (1 - np.exp(-t / .022)))
        sound = np.sin(phase) * np.exp(-t * 18) + rng.uniform(-1, 1, len(t)) * np.exp(-t * 400) * .13
    else:
        noise = rng.uniform(-1, 1, len(t)); noise = noise - np.concatenate(([0], noise[:-1])) * .78
        sound = noise * np.exp(-t * (24 if kind == "snare" else 85))
        if kind == "snare": sound = sound * .65 + np.sin(2 * np.pi * 185 * t) * np.exp(-t * 25) * .3
    return sound.astype(np.float32)


def compose(song):
    title, genre, bpm, roots, scale, genre_index = song
    beat = 60 / bpm; duration = 128 * beat; rng = np.random.default_rng(4100 + genre_index)
    mix = np.zeros((round((duration + .25) * RATE), 2), dtype=np.float32)
    def add(sound, at_beat, gain=1, pan=0):
        start = round(at_beat * beat * RATE)
        if start < 0 or start >= len(mix): return
        n = min(len(sound), len(mix) - start)
        mix[start:start+n, 0] += sound[:n] * gain * math.sqrt((1 - pan) * .5)
        mix[start:start+n, 1] += sound[:n] * gain * math.sqrt((1 + pan) * .5)
    drums = {name: drum(name, rng) for name in ("kick", "snare", "hat")}
    for bar in range(32):
        root = roots[(bar // 2) % 4]
        chorus = 12 <= bar < 20 or bar >= 24
        bridge = 20 <= bar < 24
        gain = .68 if bar < 4 else .75 if bridge else 1
        for b in range(4):
            at = bar * 4 + b
            kick = b in ((0, 2) if genre_index in (3, 4, 5) else (0, 1, 2, 3))
            if kick: add(drums["kick"], at, .75 * gain)
            if b in (1, 3): add(drums["snare"], at, .46 * gain)
            if genre_index == 6 and not bridge: add(drums["kick"], at + .5, .5)
            for half in (0, .5):
                swing = .08 if genre_index in (2, 3, 4) and half else 0
                add(drums["hat"], at + half + swing, .09 if half == 0 else .16, -.25)
            bass_note = root + (7 if b == 2 and genre_index in (2, 4) else 0)
            add(instrument(bass_note, beat * .82, "bass", rng), at, .38 * gain)
            if genre_index in (1, 2): add(instrument(root + 12, beat * .32, "bass", rng), at + .75, .17 * gain)
        chord = [0, 4 if genre_index in (1, 4) else 3, 7]
        for cb in ([0, 2] if genre_index in (0, 3) else [.5, 1.5, 2.5, 3.5]):
            style = "metal" if genre_index == 6 else "guitar" if genre_index == 5 else "twang" if genre_index == 4 else "keys"
            for interval in chord:
                add(instrument(root + 12 + interval, beat * (1.5 if genre_index in (0, 3) else .38), style, rng), bar * 4 + cb, .065 * gain, .35)
        if chorus:
            hook = HOOKS[genre_index]
            for step in range(8):
                note = root + 24 + scale[hook[(step + (bar % 2) * 4) % 8]]
                style = "twang" if genre_index == 4 else "guitar" if genre_index >= 5 else "keys"
                add(instrument(note, beat * .45, style, rng), bar * 4 + step * .5, .14, -.32)
        if bar % 8 == 7 and bar < 31:
            for fill in range(4): add(drums["snare"], bar * 4 + 3 + fill * .25, .2 + fill * .04, fill * .15 - .2)
    # Original ending, gentle common headroom and short stereo ambience. Dry
    # downbeats stay dominant for calibration and timing judgments.
    tail_start = round(126 * beat * RATE)
    mix[tail_start:] *= np.linspace(1, 0, len(mix) - tail_start, dtype=np.float32)[:, None]
    delay = round(beat * .75 * RATE)
    mix[delay:] += mix[:-delay, ::-1] * .08
    mix = np.tanh(mix * 1.1)
    mix *= .88 / max(.01, float(np.max(np.abs(mix))))
    mix = mix[:round(duration * RATE)]
    with wave.open(str(OUT / (title + ".wav")), "wb") as audio:
        audio.setnchannels(2); audio.setsampwidth(2); audio.setframerate(RATE)
        audio.writeframes(np.clip(mix * 32767, -32768, 32767).astype("<i2").tobytes())
    charts = []
    for difficulty, step in enumerate((2, 1, .5, .25)):
        notes = []
        pattern = (1, 4, 2, 8, 1, 2, 4, 8)
        for n in range(round(124 / step)):
            at = 4 + n * step
            if at >= 126 or 80 <= at < 88: continue
            lane = pattern[(n + genre_index * 2 + int(at // 16)) % 8]
            length = 0
            if difficulty >= 1 and int(at) % 16 == 12 and abs(at - round(at)) < .001:
                length = min(1.5, max(.75, step * 1.5))
            if difficulty >= 2 and n % 12 == 7:
                # Diagonal pairs are usable even on one shared analog stick.
                lane = (1 | 4, 1 | 2, 8 | 4, 8 | 2)[(n // 12 + genre_index) % 4]
            if notes and notes[-1][2] > 0 and at < notes[-1][0] + notes[-1][2] + step * .5: continue
            notes.append([round(at, 6), lane, round(length, 6)])
        charts.append({"difficulty": difficulty, "notes": notes})
    return {"title": title, "genre": genre, "bpm": bpm, "duration": duration, "sampleRate": RATE, "firstDownbeatSample": 0, "freestyleStartBeat": 80, "freestyleEndBeat": 88, "charts": charts}


if __name__ == "__main__":
    songs = []
    for song in SONGS:
        songs.append(compose(song)); print("COMPOSED", song[0], song[1], flush=True)
    (OUT / "Charts.json").write_text(json.dumps({"songs": songs}, indent=2), encoding="utf-8")
    (OUT / "OriginalAudioLicense.txt").write_text("Original Truck Sports: Party! instrumental compositions and procedural instrument patches, created for this project. No sampled third-party recordings, song transcriptions or copied melodies. Seven complete 32-bar arrangements with exact sample-derived chart grids. Composition source: ComposeDanceMusic.py. Subjective musical quality and final listening mix require human review.\n", encoding="utf-8")
    print("DONE seven original songs / 28 synchronized charts", flush=True)
