// Music Engine — ported from SqncR.Core and SqncR.Theory
// Provides: scales, chords, Euclidean rhythms, arpeggio generators, pattern library, variety engine

// ============================================================
// SCALE SYSTEM (from SqncR.Theory/Scale.cs + ScaleLibrary.cs)
// ============================================================

const SCALE_INTERVALS = {
  major:            [0, 2, 4, 5, 7, 9, 11],
  minor:            [0, 2, 3, 5, 7, 8, 10],
  harmonicMinor:    [0, 2, 3, 5, 7, 8, 11],
  melodicMinor:     [0, 2, 3, 5, 7, 9, 11],
  pentatonicMajor:  [0, 2, 4, 7, 9],
  pentatonicMinor:  [0, 3, 5, 7, 10],
  blues:            [0, 3, 5, 6, 7, 10],
  wholeTone:        [0, 2, 4, 6, 8, 10],
  diminished:       [0, 1, 3, 4, 6, 7, 9, 10],
  dorian:           [0, 2, 3, 5, 7, 9, 10],
  phrygian:         [0, 1, 3, 5, 7, 8, 10],
  lydian:           [0, 2, 4, 6, 7, 9, 11],
  mixolydian:       [0, 2, 4, 5, 7, 9, 10],
};

// Note name to MIDI pitch class
const NOTE_MAP = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };

export function parseRoot(name) {
  // e.g. "A", "C#", "Bb"
  const base = NOTE_MAP[name[0].toUpperCase()];
  if (base === undefined) return 0;
  if (name.length > 1 && (name[1] === '#' || name[1] === '♯')) return (base + 1) % 12;
  if (name.length > 1 && (name[1] === 'b' || name[1] === '♭')) return (base + 11) % 12;
  return base;
}

export function getScale(rootNote, scaleName) {
  const intervals = SCALE_INTERVALS[scaleName] || SCALE_INTERVALS.minor;
  const rootPC = rootNote % 12;
  return { rootNote, rootPC, intervals, name: scaleName };
}

export function getNotesInOctave(scale, octave) {
  const octaveBase = (octave + 1) * 12;
  return scale.intervals
    .map(i => octaveBase + scale.rootPC + i)
    .filter(n => n >= 0 && n <= 127);
}

export function getChordTones(scale, octave) {
  const notes = getNotesInOctave(scale, octave);
  const tones = [notes[0]]; // root
  if (notes.length > 2) tones.push(notes[2]); // 3rd
  if (notes.length > 4) tones.push(notes[4]); // 5th
  if (notes.length > 6) tones.push(notes[6]); // 7th
  return tones;
}

export function getExtendedChordTones(scale, octave) {
  const notes = getNotesInOctave(scale, octave);
  const upper = getNotesInOctave(scale, octave + 1);
  const tones = [notes[0]];
  if (notes.length > 2) tones.push(notes[2]);
  if (notes.length > 4) tones.push(notes[4]);
  if (notes.length > 6) tones.push(notes[6]);
  if (upper.length > 1) tones.push(upper[1]); // 9th
  return tones;
}

// ============================================================
// EUCLIDEAN RHYTHM GENERATOR (from SqncR.Core/Rhythm/EuclideanGenerator.cs)
// ============================================================

export function euclidean(steps, hits, rotation = 0) {
  if (steps <= 0) throw new Error("Steps must be positive");
  if (hits < 0 || hits > steps) throw new Error(`Hits must be 0-${steps}`);
  if (hits === 0) return new Array(steps).fill(false);
  if (hits === steps) return new Array(steps).fill(true);

  // Bjorklund's algorithm
  let pattern = Array.from({ length: hits }, () => [true]);
  let remainder = Array.from({ length: steps - hits }, () => [false]);

  while (remainder.length > 1) {
    const newPattern = [];
    const newRemainder = [];
    const minCount = Math.min(pattern.length, remainder.length);

    for (let i = 0; i < minCount; i++) {
      newPattern.push([...pattern[i], ...remainder[i]]);
    }
    const larger = pattern.length > remainder.length ? pattern : remainder;
    for (let i = minCount; i < larger.length; i++) {
      newRemainder.push(larger[i]);
    }
    pattern = newPattern;
    remainder = newRemainder;
  }

  const result = [...pattern.flat(), ...remainder.flat()];

  if (rotation !== 0) {
    const rot = ((rotation % steps) + steps) % steps;
    const rotated = new Array(steps);
    for (let i = 0; i < steps; i++) {
      rotated[(i + rot) % steps] = result[i];
    }
    return rotated;
  }
  return result;
}

// Named Euclidean presets (from Toussaint's research)
export const EUCLIDEAN_PRESETS = {
  tresillo: { steps: 8, hits: 3 },       // Cuban 3-3-2
  cinquillo: { steps: 8, hits: 5 },      // Cuban cinquillo
  bossaNova: { steps: 16, hits: 5 },     // Bossa nova
  westAfrican: { steps: 16, hits: 7 },   // West African bell
  sparse: { steps: 16, hits: 3 },        // Ambient
  dense: { steps: 16, hits: 11 },        // Driving
};

// ============================================================
// ARPEGGIO GENERATOR (from SqncR.Core/Generation/ArpeggioGenerator.cs)
// ============================================================

export function generateArpeggio(chordTones, steps, pattern = 'up', octaveSpread = 1) {
  const allTones = [];
  for (let oct = 0; oct < octaveSpread; oct++) {
    allTones.push(...chordTones.map(n => n + oct * 12).filter(n => n <= 127));
  }

  const result = [];
  const count = allTones.length;
  if (count === 0) return Array.from({ length: steps }, () => ({ active: false, note: 60, velocity: 0 }));

  for (let i = 0; i < steps; i++) {
    let index;
    switch (pattern) {
      case 'up':
        index = i % count;
        break;
      case 'down':
        index = (count - 1) - (i % count);
        break;
      case 'upDown': {
        const cycleLen = count > 1 ? (count - 1) * 2 : 1;
        const pos = i % cycleLen;
        index = pos < count ? pos : cycleLen - pos;
        break;
      }
      case 'random':
        index = Math.floor(Math.random() * count);
        break;
      default:
        index = i % count;
    }

    // Velocity variation for musicality
    const baseVel = 80 + Math.floor(Math.random() * 30);
    const accentVel = (i % 4 === 0) ? Math.min(baseVel + 20, 127) : baseVel;

    result.push({ active: true, note: allTones[index], velocity: accentVel });
  }
  return result;
}

// ============================================================
// PATTERN LIBRARY (from SqncR.Core/Rhythm/PatternLibrary.cs)
// ============================================================

function stepsFromHits(totalSteps, activeSteps, velocity) {
  return Array.from({ length: totalSteps }, (_, i) => ({
    active: activeSteps.includes(i),
    note: 36,
    velocity: activeSteps.includes(i) ? velocity : 0,
  }));
}

function stepsFromEuclidean(totalSteps, hits, velocity, rotation = 0) {
  const rhythm = euclidean(totalSteps, hits, rotation);
  return rhythm.map(hit => ({
    active: hit,
    note: 36,
    velocity: hit ? velocity : 0,
  }));
}

function accentedHat(totalSteps) {
  return Array.from({ length: totalSteps }, (_, i) => ({
    active: true,
    note: 42,
    velocity: (i % 4 === 0) ? 100 : (i % 2 === 0) ? 75 : 55,
  }));
}

export const DRUM_PATTERNS = {
  house: {
    kick: stepsFromHits(16, [0, 4, 8, 12], 120),          // four-on-the-floor
    clap: stepsFromHits(16, [4, 12], 105),                 // 2 & 4
    hat: (() => {                                           // offbeat open hats + ghost 16ths
      const p = Array.from({ length: 16 }, (_, i) => ({
        active: true,
        note: 42,
        velocity: (i % 4 === 2) ? 100 : (i % 2 === 1) ? 70 : 45,
      }));
      return p;
    })(),
  },
  breakbeat: {
    kick: stepsFromHits(16, [0, 3, 6, 10], 115),
    clap: stepsFromHits(16, [4, 12], 110),
    hat: accentedHat(16),
  },
  ambient: {
    kick: stepsFromEuclidean(16, 3, 90),
    clap: stepsFromEuclidean(16, 2, 70, 4),
    hat: stepsFromEuclidean(16, 5, 60, 2),
  },
  latin: {
    kick: stepsFromHits(16, [0, 4, 10], 100),
    clap: stepsFromHits(16, [2, 6, 10, 14], 85),           // cross-stick
    hat: stepsFromHits(16, [0, 2, 4, 6, 8, 10, 12, 14], 65),
  },
  euclidean: {
    kick: stepsFromEuclidean(16, 5, 110),
    clap: stepsFromEuclidean(16, 3, 95, 2),
    hat: stepsFromEuclidean(16, 9, 75, 1),
  },
};

// ============================================================
// BASS LINE GENERATOR
// ============================================================

export function generateBassLine(scale, octave, steps, style = 'pumping') {
  const notes = getNotesInOctave(scale, octave);
  const chordTones = [notes[0], notes[2 % notes.length], notes[4 % notes.length]];

  switch (style) {
    case 'pumping': {
      // Classic house pump — root heavy with passing tones
      const pattern = [];
      for (let i = 0; i < steps; i++) {
        if (i % 4 === 0) {
          pattern.push({ active: true, note: chordTones[0], velocity: 115 });
        } else if (i % 4 === 2) {
          pattern.push({ active: true, note: chordTones[0], velocity: 85 });
        } else if (i % 8 === 3) {
          pattern.push({ active: true, note: chordTones[1], velocity: 75 });
        } else if (i % 8 === 7) {
          pattern.push({ active: true, note: chordTones[2], velocity: 70 });
        } else {
          pattern.push({ active: false, note: chordTones[0], velocity: 0 });
        }
      }
      return pattern;
    }
    case 'walking': {
      // Walking bass through scale degrees
      return Array.from({ length: steps }, (_, i) => ({
        active: true,
        note: notes[i % notes.length],
        velocity: 80 + (i % 4 === 0 ? 25 : Math.floor(Math.random() * 15)),
      }));
    }
    case 'octave': {
      // Octave bounce
      const low = chordTones[0];
      const high = low + 12;
      return Array.from({ length: steps }, (_, i) => ({
        active: i % 2 === 0 || i % 8 === 3,
        note: (i % 4 < 2) ? low : high,
        velocity: i % 4 === 0 ? 110 : 80,
      }));
    }
    default:
      return generateBassLine(scale, octave, steps, 'pumping');
  }
}

// ============================================================
// MELODY / LEAD GENERATOR
// ============================================================

export function generateMelody(scale, octave, steps, style = 'arpeggiated') {
  const chordTones = getExtendedChordTones(scale, octave);
  const allNotes = getNotesInOctave(scale, octave);
  const upperNotes = getNotesInOctave(scale, octave + 1);

  switch (style) {
    case 'arpeggiated':
      return generateArpeggio(chordTones, steps, 'upDown', 1);
    case 'scalar': {
      const combined = [...allNotes, ...upperNotes.slice(0, 3)];
      return Array.from({ length: steps }, (_, i) => {
        const shouldRest = Math.random() < 0.2;
        if (shouldRest) return { active: false, note: 60, velocity: 0 };
        const idx = Math.floor(Math.random() * combined.length);
        return { active: true, note: combined[idx], velocity: 70 + Math.floor(Math.random() * 40) };
      });
    }
    case 'call-response': {
      // 4 steps phrase, 4 steps rest, repeat with variation
      const result = [];
      for (let i = 0; i < steps; i++) {
        const phrasePos = i % 8;
        if (phrasePos < 4) {
          const note = chordTones[phrasePos % chordTones.length];
          result.push({ active: true, note, velocity: 90 + Math.floor(Math.random() * 25) });
        } else {
          result.push({ active: false, note: 60, velocity: 0 });
        }
      }
      return result;
    }
    default:
      return generateMelody(scale, octave, steps, 'arpeggiated');
  }
}

// ============================================================
// PAD GENERATOR
// ============================================================

export function generatePadPattern(scale, octave, steps) {
  const chordTones = getExtendedChordTones(scale, octave);
  // Pads sustain — arpeggiate slowly with long notes
  return generateArpeggio(chordTones, steps, 'upDown', 2);
}

// ============================================================
// VARIETY ENGINE (from SqncR.Core/Generation/VarietyEngine.cs)
// ============================================================

export class VarietyEngine {
  constructor(level = 'moderate') {
    this.level = level;
    this.octaveDrift = 0;
    this.velocityDrift = 0;
    this.measureCount = 0;
  }

  get probability() {
    switch (this.level) {
      case 'conservative': return 0.05;
      case 'moderate': return 0.15;
      case 'adventurous': return 0.30;
      default: return 0.15;
    }
  }

  // Mutate a pattern for a given measure (returns new pattern)
  evolvePattern(pattern, measure) {
    this.measureCount = measure;
    const evolved = pattern.map(step => ({ ...step }));

    // Velocity drift every ~8 measures
    if (measure > 0 && measure % 8 === 0 && Math.random() < this.probability) {
      this.velocityDrift = (Math.random() < 0.5 ? 1 : -1) * (10 + Math.floor(Math.random() * 15));
    }
    if (measure % 16 === 0) this.velocityDrift = 0; // reset

    // Apply velocity drift
    if (this.velocityDrift !== 0) {
      for (const step of evolved) {
        if (step.active) {
          step.velocity = Math.max(30, Math.min(127, step.velocity + this.velocityDrift));
        }
      }
    }

    // Ghost note insertion (every 4 measures)
    if (measure > 0 && measure % 4 === 0 && Math.random() < this.probability) {
      const rests = evolved.map((s, i) => !s.active ? i : -1).filter(i => i >= 0);
      if (rests.length > 0) {
        const idx = rests[Math.floor(Math.random() * rests.length)];
        evolved[idx] = { active: true, note: evolved[Math.max(0, idx - 1)].note, velocity: 40 + Math.floor(Math.random() * 20) };
      }
    }

    return evolved;
  }
}

// ============================================================
// FULL PATCH GENERATOR — orchestrates everything
// ============================================================

export function generateFullPatch({ key = 'A', scale: scaleName = 'minor', bpm = 128, style = 'house', steps = 16 } = {}) {
  const rootPC = parseRoot(key);
  const scale = getScale(rootPC, scaleName);

  // Select drum pattern
  const drumPattern = DRUM_PATTERNS[style] || DRUM_PATTERNS.house;

  // Generate melodic content
  const bassLine = generateBassLine(scale, 2, steps, style === 'house' ? 'pumping' : 'walking');
  const padPattern = generatePadPattern(scale, 3, steps);
  const leadMelody = generateMelody(scale, 4, steps, 'arpeggiated');

  return {
    bpm,
    key,
    scale: scaleName,
    style,
    drums: drumPattern,
    bass: bassLine,
    pad: padPattern,
    lead: leadMelody,
  };
}
