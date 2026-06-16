// Extension: modular-synth
// Modular software synthesizer with oscillators, sequencer, drums, virtual patching, MIDI, effects, polyphony, and presets
// Controllable programmatically via canvas actions for SqncR integration

import { createServer } from "node:http";
import { readFileSync, readdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { WebSocketServer } from "ws";
import { joinSession, createCanvas } from "@github/copilot-sdk/extension";
import { generateFullPatch, euclidean, generateArpeggio, getScale, parseRoot, getChordTones, getNotesInOctave, DRUM_PATTERNS, generateBassLine, generateMelody, generatePadPattern } from "./music-engine.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));

const servers = new Map();

// ============================================================
// PRESET LOADER — reads from presets/ directory
// ============================================================
const PRESETS_DIR = join(__dirname, "presets");

function loadPresetIndex() {
    const files = readdirSync(PRESETS_DIR).filter(f => f.endsWith('.json'));
    return files.map(f => {
        const data = JSON.parse(readFileSync(join(PRESETS_DIR, f), 'utf-8'));
        return data;
    });
}

function findPreset(query) {
    const presets = loadPresetIndex();
    const q = query.toLowerCase();
    // Exact id match
    const exact = presets.find(p => p.id === q);
    if (exact) return exact;
    // Name match
    const byName = presets.find(p => p.name.toLowerCase().includes(q));
    if (byName) return byName;
    // Tag match
    const byTag = presets.find(p => p.tags.some(t => t.toLowerCase().includes(q)));
    if (byTag) return byTag;
    // Genre/mood/description match
    const byMeta = presets.find(p =>
        p.genre?.toLowerCase().includes(q) ||
        p.mood?.toLowerCase().includes(q) ||
        p.description?.toLowerCase().includes(q)
    );
    return byMeta || null;
}

function getSynthHtml(wsPort) {
    let html = readFileSync(join(__dirname, "synth.html"), "utf-8");
    // Inject WebSocket connection for programmatic control
    const wsScript = `
<script>
// ============================================================
// REMOTE CONTROL VIA WEBSOCKET (SqncR Integration)
// ============================================================
(function() {
    const wsUrl = 'ws://' + window.location.host;
    const ws = new WebSocket(wsUrl);
    ws.onopen = () => console.log('[SqncR] Connected to control bus');
    ws.onmessage = (event) => {
        try {
            const cmd = JSON.parse(event.data);
            const result = handleCommand(cmd);
            ws.send(JSON.stringify({ id: cmd.id, result }));
        } catch(e) {
            ws.send(JSON.stringify({ id: null, error: e.message }));
        }
    };

    function handleCommand(cmd) {
        switch(cmd.type) {
            case 'add_module': return cmdAddModule(cmd);
            case 'remove_module': return cmdRemoveModule(cmd);
            case 'connect': return cmdConnect(cmd);
            case 'disconnect': return cmdDisconnect(cmd);
            case 'set_param': return cmdSetParam(cmd);
            case 'set_bpm': return cmdSetBpm(cmd);
            case 'play': getAudioCtx(); transport.play(); return { ok: true };
            case 'stop': transport.stop(); return { ok: true };
            case 'note_on': return cmdNoteOn(cmd);
            case 'note_off': return cmdNoteOff(cmd);
            case 'load_patch': return cmdLoadPatch(cmd);
            case 'get_state': return cmdGetState();
            case 'trigger_drum': return cmdTriggerDrum(cmd);
            case 'set_sequence': return cmdSetSequence(cmd);
            default: return { error: 'Unknown command: ' + cmd.type };
        }
    }

    function cmdAddModule(cmd) {
        const p = nextPos();
        const x = cmd.x ?? p.x;
        const y = cmd.y ?? p.y;
        let mod;
        switch(cmd.module_type) {
            case 'osc': mod = new OscillatorModule(x, y); break;
            case 'seq': mod = new SequencerModule(x, y); break;
            case 'drum': mod = new DrumModule(x, y); break;
            case 'env': mod = new EnvelopeModule(x, y); break;
            case 'vca': mod = new VCAModule(x, y); break;
            case 'mixer': mod = new MixerModule(x, y); break;
            case 'reverb': mod = new ReverbEffect(x, y); break;
            case 'delay': mod = new DelayEffect(x, y); break;
            case 'filter': mod = new FilterEffect(x, y); break;
            case 'lfo': mod = new LFOModule(x, y); break;
            case 'chorus': mod = new ChorusEffect(x, y); break;
            case 'phaser': mod = new PhaserEffect(x, y); break;
            case 'compressor': mod = new CompressorEffect(x, y); break;
            case 'panner': mod = new PannerModule(x, y); break;
            default: return { error: 'Unknown module type: ' + cmd.module_type };
        }
        // Apply initial params if provided
        if (cmd.params) {
            Object.entries(cmd.params).forEach(([k, v]) => {
                mod.params[k] = v;
                mod.onParamChange(k, v);
            });
        }
        if (cmd.waveform && mod.waveform !== undefined) {
            mod.waveform = cmd.waveform;
            mod.voices.forEach(v => v.osc.type = cmd.waveform);
        }
        return { ok: true, module_id: mod.id };
    }

    function cmdRemoveModule(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod) return { error: 'Module not found: ' + cmd.module_id };
        mod.destroy();
        return { ok: true };
    }

    function cmdConnect(cmd) {
        const fromMod = moduleRegistry.get(cmd.from_module);
        const toMod = moduleRegistry.get(cmd.to_module);
        if (!fromMod) return { error: 'Source module not found: ' + cmd.from_module };
        if (!toMod) return { error: 'Dest module not found: ' + cmd.to_module };
        const cable = new PatchCable(cmd.from_module, cmd.from_jack, cmd.to_module, cmd.to_jack);
        patchBay.cables.push(cable);
        patchBay._connectAudio(cable);
        patchBay._renderCable(cable);
        return { ok: true, cable_id: cable.id };
    }

    function cmdDisconnect(cmd) {
        patchBay.removeCable(cmd.cable_id);
        return { ok: true };
    }

    function cmdSetParam(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod) return { error: 'Module not found: ' + cmd.module_id };
        mod.params[cmd.param] = cmd.value;
        mod.onParamChange(cmd.param, cmd.value);
        return { ok: true };
    }

    function cmdSetBpm(cmd) {
        transport.setBpm(cmd.bpm);
        document.getElementById('bpm-input').value = cmd.bpm;
        return { ok: true };
    }

    function cmdNoteOn(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod || !mod._noteOn) return { error: 'Module not found or does not support notes' };
        mod._noteOn(cmd.note, cmd.velocity ?? 100);
        return { ok: true };
    }

    function cmdNoteOff(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod || !mod._noteOff) return { error: 'Module not found or does not support notes' };
        mod._noteOff(cmd.note);
        return { ok: true };
    }

    function cmdTriggerDrum(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod || !mod.trigger) return { error: 'Module not found or is not a drum' };
        mod.trigger(cmd.velocity ?? 100);
        return { ok: true };
    }

    function cmdSetSequence(cmd) {
        const mod = moduleRegistry.get(cmd.module_id);
        if (!mod || !mod.pattern) return { error: 'Module not found or is not a sequencer' };
        if (cmd.pattern) mod.pattern = cmd.pattern;
        if (cmd.steps) mod.params.steps = cmd.steps;
        return { ok: true };
    }

    function cmdLoadPatch(cmd) {
        presetMgr._restoreState(cmd.patch);
        return { ok: true };
    }

    function cmdGetState() {
        const modules = [];
        moduleRegistry.forEach((mod) => modules.push({ id: mod.id, type: mod.type, params: { ...mod.params } }));
        return {
            ok: true,
            state: {
                modules,
                cables: patchBay.serialize(),
                transport: { bpm: transport.bpm, playing: transport.playing }
            }
        };
    }

    // Expose for debugging
    window._sqncrWs = ws;
})();
</script>`;
    // Insert before closing </body>
    html = html.replace('</body>', wsScript + '\n</body>');
    return html;
}

async function startServer(instanceId) {
    // Track connected clients and pending requests
    const clients = new Set();
    const pending = new Map();
    let reqId = 0;

    // HTTP server for the synth UI — WebSocket upgrades on the SAME port (same-origin)
    const server = createServer((req, res) => {
        // API: serve preset list as JSON
        if (req.url === '/api/presets') {
            const presets = loadPresetIndex();
            res.setHeader("Content-Type", "application/json");
            res.end(JSON.stringify(presets));
            return;
        }
        const html = getSynthHtml(0); // port 0 = same-origin WS
        res.setHeader("Content-Type", "text/html; charset=utf-8");
        res.end(html);
    });

    // WebSocket server attached to the HTTP server (same port, no CORS issues)
    const wss = new WebSocketServer({ server });

    wss.on("connection", (ws) => {
        clients.add(ws);
        ws.on("close", () => clients.delete(ws));
        ws.on("message", async (data) => {
            try {
                const msg = JSON.parse(data);
                // Handle community preset load from the browser UI
                if (msg.type === 'load_community_preset' && msg.preset_id) {
                    const preset = findPreset(msg.preset_id);
                    if (!preset) {
                        ws.send(JSON.stringify({ id: msg.id, error: 'Preset not found' }));
                        return;
                    }
                    // Generate patch from preset params and send commands
                    const patch = generateFullPatch({
                        key: preset.key || 'A',
                        scale: preset.scale || 'minor',
                        bpm: preset.bpm || 128,
                        style: preset.style || 'house',
                        steps: 16,
                    });
                    const rootPC = parseRoot(preset.key || 'A');
                    const scale = getScale(rootPC, preset.scale || 'minor');
                    if (preset.bass_style) patch.bass = generateBassLine(scale, 2, 16, preset.bass_style);
                    if (preset.lead_style) patch.lead = generateMelody(scale, 4, 16, preset.lead_style);

                    // Build the full patch from the UI
                    const send = sendCommand;
                    await send({ type: "set_bpm", bpm: patch.bpm });

                    const kick = await send({ type: "add_module", module_type: "drum", params: { decay: 0.4, tone: 0.3 } });
                    const clap = await send({ type: "add_module", module_type: "drum", params: { decay: 0.12, tone: 0.85 } });
                    const hat = await send({ type: "add_module", module_type: "drum", params: { decay: 0.08, tone: 0.6 } });
                    const seqKick = await send({ type: "add_module", module_type: "seq" });
                    const seqClap = await send({ type: "add_module", module_type: "seq" });
                    const seqHat = await send({ type: "add_module", module_type: "seq" });
                    const bassOsc = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: -1 } });
                    const seqBass = await send({ type: "add_module", module_type: "seq" });
                    const padOsc1 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: 6 } });
                    const padOsc2 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: -6 } });
                    const seqPad1 = await send({ type: "add_module", module_type: "seq" });
                    const seqPad2 = await send({ type: "add_module", module_type: "seq" });
                    const leadOsc = await send({ type: "add_module", module_type: "osc", waveform: "triangle", params: { octave: 1 } });
                    const seqLead = await send({ type: "add_module", module_type: "seq" });
                    const bassFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 500, resonance: 4 } });
                    const padFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 1200, resonance: 2 } });
                    const leadFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 2500, resonance: 1.5 } });
                    const lfo = await send({ type: "add_module", module_type: "lfo", waveform: "sine", params: { rate: 0.2 } });
                    const chorus = await send({ type: "add_module", module_type: "chorus", params: { rate: 0.7, depth: 0.4, mix: 0.45 } });
                    const reverb = await send({ type: "add_module", module_type: "reverb", params: { decay: 3.5, mix: 0.5 } });
                    const delay = await send({ type: "add_module", module_type: "delay", params: { time: 0.375, feedback: 0.4, mix: 0.3 } });
                    const drumMixer = await send({ type: "add_module", module_type: "mixer" });
                    const padMixer = await send({ type: "add_module", module_type: "mixer" });
                    const compressor = await send({ type: "add_module", module_type: "compressor", params: { threshold: -10, ratio: 4, attack: 0.005, release: 0.15 } });

                    const id = (r) => r && r.module_id ? r.module_id : null;

                    // Program sequences
                    await send({ type: "set_sequence", module_id: id(seqKick), pattern: patch.drums.kick, steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqClap), pattern: patch.drums.clap, steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqHat), pattern: patch.drums.hat, steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqBass), pattern: patch.bass, steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqPad1), pattern: patch.pad, steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqPad2), pattern: patch.pad.map(s => ({ ...s, note: s.note ? s.note + 4 : s.note })), steps: 16 });
                    await send({ type: "set_sequence", module_id: id(seqLead), pattern: patch.lead, steps: 16 });

                    // Wire up
                    await send({ type: "connect", from_module: id(seqKick), from_jack: "gate_out", to_module: id(kick), to_jack: "gate" });
                    await send({ type: "connect", from_module: id(seqClap), from_jack: "gate_out", to_module: id(clap), to_jack: "gate" });
                    await send({ type: "connect", from_module: id(seqHat), from_jack: "gate_out", to_module: id(hat), to_jack: "gate" });
                    await send({ type: "connect", from_module: id(seqBass), from_jack: "cv_out", to_module: id(bassOsc), to_jack: "cv_in" });
                    await send({ type: "connect", from_module: id(seqPad1), from_jack: "cv_out", to_module: id(padOsc1), to_jack: "cv_in" });
                    await send({ type: "connect", from_module: id(seqPad2), from_jack: "cv_out", to_module: id(padOsc2), to_jack: "cv_in" });
                    await send({ type: "connect", from_module: id(seqLead), from_jack: "cv_out", to_module: id(leadOsc), to_jack: "cv_in" });
                    await send({ type: "connect", from_module: id(kick), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch1" });
                    await send({ type: "connect", from_module: id(clap), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch2" });
                    await send({ type: "connect", from_module: id(hat), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch3" });
                    await send({ type: "connect", from_module: id(bassOsc), from_jack: "audio_out", to_module: id(bassFilter), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(padOsc1), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch1" });
                    await send({ type: "connect", from_module: id(padOsc2), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch2" });
                    await send({ type: "connect", from_module: id(padMixer), from_jack: "audio_out", to_module: id(padFilter), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(lfo), from_jack: "cv_out", to_module: id(padFilter), to_jack: "cv_in" });
                    await send({ type: "connect", from_module: id(padFilter), from_jack: "audio_out", to_module: id(chorus), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(chorus), from_jack: "audio_out", to_module: id(reverb), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(leadOsc), from_jack: "audio_out", to_module: id(leadFilter), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(leadFilter), from_jack: "audio_out", to_module: id(delay), to_jack: "audio_in" });
                    await send({ type: "connect", from_module: id(drumMixer), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch1" });
                    await send({ type: "connect", from_module: id(bassFilter), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch2" });
                    await send({ type: "connect", from_module: id(reverb), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch3" });
                    await send({ type: "connect", from_module: id(delay), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch4" });

                    await send({ type: "play" });

                    ws.send(JSON.stringify({ id: msg.id, result: { ok: true, preset_loaded: preset.id } }));
                    return;
                }
                if (msg.id && pending.has(msg.id)) {
                    pending.get(msg.id)(msg.result || msg.error);
                    pending.delete(msg.id);
                }
            } catch {}
        });
    });

    // Send command to synth and await response
    function sendCommand(cmd, timeoutMs = 3000) {
        return new Promise((resolve, reject) => {
            const id = ++reqId;
            cmd.id = id;
            const msg = JSON.stringify(cmd);
            let sent = false;
            for (const client of clients) {
                client.send(msg);
                sent = true;
            }
            if (!sent) {
                resolve({ error: "No synth UI connected. Please click on the SqncR panel to activate it." });
                return;
            }
            const timer = setTimeout(() => {
                pending.delete(id);
                resolve({ ok: true, message: "Command sent (no ack within timeout)" });
            }, timeoutMs);
            pending.set(id, (result) => {
                clearTimeout(timer);
                resolve(result);
            });
        });
    }

    const SQNCR_PORT = 19840;
    await new Promise((resolve, reject) => {
        server.once('error', (err) => {
            if (err.code === 'EADDRINUSE') {
                // Fallback to random port if preferred port is taken
                server.listen(0, "127.0.0.1", resolve);
            } else {
                reject(err);
            }
        });
        // Suppress WSS error during listen (it mirrors the server error)
        wss.on('error', () => {});
        server.listen(SQNCR_PORT, "127.0.0.1", () => {
            server.removeAllListeners('error');
            resolve();
        });
    });
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;

    return { server, wss, sendCommand, url: `http://127.0.0.1:${port}/` };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "sqncr",
            displayName: "SqncR",
            description:
                "A modular software synthesizer with oscillators, sequencer, drum voices, virtual patch cables, MIDI I/O with clock sync, effects (reverb, delay), polyphony, and preset save/load. Fully controllable programmatically — build patches, trigger notes, and modify parameters via actions.",
            actions: [
                {
                    name: "add_module",
                    description: "Add a module to the synth rack and return its ID",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_type: {
                                type: "string",
                                enum: ["osc", "seq", "drum", "env", "vca", "mixer", "reverb", "delay", "filter", "lfo", "chorus", "phaser", "compressor", "panner"],
                                description: "Type of module to add",
                            },
                            params: {
                                type: "object",
                                description: "Optional initial parameter values (e.g. {octave: -1, fine: 0})",
                            },
                            waveform: {
                                type: "string",
                                enum: ["sine", "triangle", "sawtooth", "square"],
                                description: "Waveform for oscillator modules",
                            },
                        },
                        required: ["module_type"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "add_module", ...ctx.input });
                    },
                },
                {
                    name: "remove_module",
                    description: "Remove a module from the rack by ID",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Module ID to remove" },
                        },
                        required: ["module_id"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "remove_module", module_id: ctx.input.module_id });
                    },
                },
                {
                    name: "connect",
                    description: "Patch a cable between two module jacks (e.g. osc audio_out → mixer ch1)",
                    inputSchema: {
                        type: "object",
                        properties: {
                            from_module: { type: "string", description: "Source module ID" },
                            from_jack: { type: "string", description: "Source output jack name (e.g. audio_out, cv_out, gate_out)" },
                            to_module: { type: "string", description: "Destination module ID" },
                            to_jack: { type: "string", description: "Destination input jack name (e.g. ch1, audio_in, gate)" },
                        },
                        required: ["from_module", "from_jack", "to_module", "to_jack"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "connect", ...ctx.input });
                    },
                },
                {
                    name: "set_param",
                    description: "Set a parameter on a module (e.g. cutoff, decay, mix, octave)",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Module ID" },
                            param: { type: "string", description: "Parameter name" },
                            value: { type: "number", description: "Parameter value" },
                        },
                        required: ["module_id", "param", "value"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "set_param", ...ctx.input });
                    },
                },
                {
                    name: "play",
                    description: "Start the transport (sequencer begins playing)",
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "play" });
                    },
                },
                {
                    name: "stop",
                    description: "Stop the transport",
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "stop" });
                    },
                },
                {
                    name: "set_bpm",
                    description: "Set the tempo in BPM",
                    inputSchema: {
                        type: "object",
                        properties: {
                            bpm: { type: "number", description: "Beats per minute (20-300)" },
                        },
                        required: ["bpm"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "set_bpm", bpm: ctx.input.bpm });
                    },
                },
                {
                    name: "note_on",
                    description: "Trigger a note on a module (MIDI note number 0-127)",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Target oscillator module ID" },
                            note: { type: "number", description: "MIDI note number (60 = middle C)" },
                            velocity: { type: "number", description: "Velocity 0-127 (default 100)" },
                        },
                        required: ["module_id", "note"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "note_on", ...ctx.input });
                    },
                },
                {
                    name: "note_off",
                    description: "Release a note on a module",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Target oscillator module ID" },
                            note: { type: "number", description: "MIDI note number to release" },
                        },
                        required: ["module_id", "note"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "note_off", ...ctx.input });
                    },
                },
                {
                    name: "trigger_drum",
                    description: "Trigger a drum voice module",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Drum module ID" },
                            velocity: { type: "number", description: "Hit velocity 0-127 (default 100)" },
                        },
                        required: ["module_id"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "trigger_drum", ...ctx.input });
                    },
                },
                {
                    name: "set_sequence",
                    description: "Program a step sequence pattern",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_id: { type: "string", description: "Sequencer module ID" },
                            pattern: {
                                type: "array",
                                description: "Array of step objects: [{note: 60, active: true, velocity: 100}, ...]",
                                items: {
                                    type: "object",
                                    properties: {
                                        note: { type: "number" },
                                        active: { type: "boolean" },
                                        velocity: { type: "number" },
                                    },
                                },
                            },
                            steps: { type: "number", description: "Number of active steps (1-16)" },
                        },
                        required: ["module_id", "pattern"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "set_sequence", ...ctx.input });
                    },
                },
                {
                    name: "load_patch",
                    description: "Load a complete patch (modules + cables + transport) from a JSON state object",
                    inputSchema: {
                        type: "object",
                        properties: {
                            patch: {
                                type: "object",
                                description: "Full patch state with modules, cables, and transport",
                            },
                        },
                        required: ["patch"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "load_patch", patch: ctx.input.patch });
                    },
                },
                {
                    name: "get_state",
                    description: "Get current synth state (all modules, connections, transport)",
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };
                        return await entry.sendCommand({ type: "get_state" });
                    },
                },
                {
                    name: "generate_patch",
                    description: "Use SqncR's music theory engine to generate a complete, musically-intelligent patch. Automatically creates modules, sequences, and cables based on key, scale, tempo, and style. Produces proper chord voicings, Euclidean rhythms, and arpeggios.",
                    inputSchema: {
                        type: "object",
                        properties: {
                            key: { type: "string", description: "Root key (e.g. 'A', 'C#', 'Eb'). Default: 'A'" },
                            scale: { type: "string", enum: ["major", "minor", "harmonicMinor", "melodicMinor", "pentatonicMajor", "pentatonicMinor", "blues", "dorian", "phrygian", "lydian", "mixolydian"], description: "Scale type. Default: 'minor'" },
                            bpm: { type: "number", description: "Tempo in BPM (20-300). Default: 128" },
                            style: { type: "string", enum: ["house", "breakbeat", "ambient", "latin", "euclidean"], description: "Drum pattern style. Default: 'house'" },
                            bass_style: { type: "string", enum: ["pumping", "walking", "octave"], description: "Bass line style. Default: 'pumping'" },
                            lead_style: { type: "string", enum: ["arpeggiated", "scalar", "call-response"], description: "Lead melody style. Default: 'arpeggiated'" },
                        },
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };

                        const input = ctx.input || {};
                        const patch = generateFullPatch({
                            key: input.key || 'A',
                            scale: input.scale || 'minor',
                            bpm: input.bpm || 128,
                            style: input.style || 'house',
                            steps: 16,
                        });

                        // Override bass/lead styles if provided
                        const rootPC = parseRoot(input.key || 'A');
                        const scale = getScale(rootPC, input.scale || 'minor');
                        if (input.bass_style) {
                            patch.bass = generateBassLine(scale, 2, 16, input.bass_style);
                        }
                        if (input.lead_style) {
                            patch.lead = generateMelody(scale, 4, 16, input.lead_style);
                        }

                        const send = entry.sendCommand.bind(entry);

                        // Set BPM
                        await send({ type: "set_bpm", bpm: patch.bpm });

                        // Create drums
                        const kick = await send({ type: "add_module", module_type: "drum", params: { decay: 0.4, tone: 0.3 } });
                        const clap = await send({ type: "add_module", module_type: "drum", params: { decay: 0.12, tone: 0.85 } });
                        const hat = await send({ type: "add_module", module_type: "drum", params: { decay: 0.08, tone: 0.6 } });

                        // Create sequencers for drums
                        const seqKick = await send({ type: "add_module", module_type: "seq" });
                        const seqClap = await send({ type: "add_module", module_type: "seq" });
                        const seqHat = await send({ type: "add_module", module_type: "seq" });

                        // Create bass osc + sequencer
                        const bassOsc = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: -1 } });
                        const seqBass = await send({ type: "add_module", module_type: "seq" });

                        // Create pad oscs (detuned pair)
                        const padOsc1 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: 6 } });
                        const padOsc2 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: -6 } });
                        const seqPad1 = await send({ type: "add_module", module_type: "seq" });
                        const seqPad2 = await send({ type: "add_module", module_type: "seq" });

                        // Create lead osc + sequencer
                        const leadOsc = await send({ type: "add_module", module_type: "osc", waveform: "triangle", params: { octave: 1 } });
                        const seqLead = await send({ type: "add_module", module_type: "seq" });

                        // Create filters
                        const bassFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 500, resonance: 4 } });
                        const padFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 1200, resonance: 2 } });
                        const leadFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 2500, resonance: 1.5 } });

                        // LFO for pad filter sweep
                        const lfo = await send({ type: "add_module", module_type: "lfo", waveform: "sine", params: { rate: 0.2 } });

                        // Effects
                        const chorus = await send({ type: "add_module", module_type: "chorus", params: { rate: 0.7, depth: 0.4, mix: 0.45 } });
                        const reverb = await send({ type: "add_module", module_type: "reverb", params: { decay: 3.5, mix: 0.5 } });
                        const delay = await send({ type: "add_module", module_type: "delay", params: { time: 0.375, feedback: 0.4, mix: 0.3 } });

                        // Mixers
                        const drumMixer = await send({ type: "add_module", module_type: "mixer" });
                        const padMixer = await send({ type: "add_module", module_type: "mixer" });

                        // Master compressor
                        const compressor = await send({ type: "add_module", module_type: "compressor", params: { threshold: -10, ratio: 4, attack: 0.005, release: 0.15 } });

                        // Helper to get module_id from result
                        const id = (r) => r && r.module_id ? r.module_id : null;

                        // Program sequences using music engine output
                        await send({ type: "set_sequence", module_id: id(seqKick), pattern: patch.drums.kick, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqClap), pattern: patch.drums.clap, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqHat), pattern: patch.drums.hat, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqBass), pattern: patch.bass, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqPad1), pattern: patch.pad, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqPad2), pattern: patch.pad.map(s => ({ ...s, note: s.note ? s.note + 4 : s.note })), steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqLead), pattern: patch.lead, steps: 16 });

                        // Wire up: sequencers → instruments
                        await send({ type: "connect", from_module: id(seqKick), from_jack: "gate_out", to_module: id(kick), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqClap), from_jack: "gate_out", to_module: id(clap), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqHat), from_jack: "gate_out", to_module: id(hat), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqBass), from_jack: "cv_out", to_module: id(bassOsc), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqPad1), from_jack: "cv_out", to_module: id(padOsc1), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqPad2), from_jack: "cv_out", to_module: id(padOsc2), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqLead), from_jack: "cv_out", to_module: id(leadOsc), to_jack: "cv_in" });

                        // Drums → drum mixer
                        await send({ type: "connect", from_module: id(kick), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(clap), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(hat), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch3" });

                        // Bass → filter
                        await send({ type: "connect", from_module: id(bassOsc), from_jack: "audio_out", to_module: id(bassFilter), to_jack: "audio_in" });

                        // Pad oscs → pad mixer → filter → chorus → reverb
                        await send({ type: "connect", from_module: id(padOsc1), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(padOsc2), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(padMixer), from_jack: "audio_out", to_module: id(padFilter), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(lfo), from_jack: "cv_out", to_module: id(padFilter), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(padFilter), from_jack: "audio_out", to_module: id(chorus), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(chorus), from_jack: "audio_out", to_module: id(reverb), to_jack: "audio_in" });

                        // Lead → filter → delay
                        await send({ type: "connect", from_module: id(leadOsc), from_jack: "audio_out", to_module: id(leadFilter), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(leadFilter), from_jack: "audio_out", to_module: id(delay), to_jack: "audio_in" });

                        // Everything → master compressor
                        await send({ type: "connect", from_module: id(drumMixer), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(bassFilter), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(reverb), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch3" });
                        await send({ type: "connect", from_module: id(delay), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch4" });

                        // Start transport
                        await send({ type: "play" });

                        return {
                            ok: true,
                            patch_info: {
                                key: patch.key,
                                scale: patch.scale,
                                bpm: patch.bpm,
                                style: patch.style,
                                modules_created: 21,
                                signal_chain: "Drums(kick+clap+hat) → Mixer → Compressor | Bass(saw→LP filter) → Compressor | Pad(2×detuned saw→filter+LFO→chorus→reverb) → Compressor | Lead(tri→HP filter→delay) → Compressor"
                            }
                        };
                    },
                },
                {
                    name: "list_presets",
                    description: "List all available preset patches with metadata. Supports filtering by tag, genre, mood, energy level, or key. Returns preset IDs, names, descriptions, and all metadata for easy discovery.",
                    inputSchema: {
                        type: "object",
                        properties: {
                            filter: { type: "string", description: "Optional search filter — matches against tags, genre, mood, energy, key, scale, name, or description" },
                        },
                    },
                    handler: async (ctx) => {
                        const presets = loadPresetIndex();
                        const filter = ctx.input?.filter?.toLowerCase();

                        const results = filter
                            ? presets.filter(p =>
                                p.name.toLowerCase().includes(filter) ||
                                p.tags.some(t => t.includes(filter)) ||
                                p.genre?.toLowerCase().includes(filter) ||
                                p.mood?.toLowerCase().includes(filter) ||
                                p.energy?.toLowerCase().includes(filter) ||
                                p.key?.toLowerCase() === filter ||
                                p.scale?.toLowerCase().includes(filter) ||
                                p.description?.toLowerCase().includes(filter)
                            )
                            : presets;

                        return {
                            ok: true,
                            count: results.length,
                            presets: results.map(p => ({
                                id: p.id,
                                name: p.name,
                                description: p.description,
                                author: p.author,
                                genre: p.genre,
                                mood: p.mood,
                                energy: p.energy,
                                bpm: p.bpm,
                                key: p.key,
                                scale: p.scale,
                                style: p.style,
                                tags: p.tags,
                            })),
                        };
                    },
                },
                {
                    name: "load_preset",
                    description: "Load a preset by ID, name, or search term. Clears the current patch and generates a fresh one using the preset's musical parameters.",
                    inputSchema: {
                        type: "object",
                        properties: {
                            query: { type: "string", description: "Preset ID, name, tag, or search term to find and load" },
                        },
                        required: ["query"],
                    },
                    handler: async (ctx) => {
                        const entry = servers.get(ctx.instanceId);
                        if (!entry) return { error: "Synth not running" };

                        const preset = findPreset(ctx.input.query);
                        if (!preset) {
                            const all = loadPresetIndex();
                            return { error: `Preset not found: "${ctx.input.query}". Available: ${all.map(p => p.id).join(', ')}` };
                        }

                        // Use generate_patch logic with preset params
                        const patch = generateFullPatch({
                            key: preset.key || 'A',
                            scale: preset.scale || 'minor',
                            bpm: preset.bpm || 128,
                            style: preset.style || 'house',
                            steps: 16,
                        });

                        const rootPC = parseRoot(preset.key || 'A');
                        const scale = getScale(rootPC, preset.scale || 'minor');
                        if (preset.bass_style) {
                            patch.bass = generateBassLine(scale, 2, 16, preset.bass_style);
                        }
                        if (preset.lead_style) {
                            patch.lead = generateMelody(scale, 4, 16, preset.lead_style);
                        }

                        const send = entry.sendCommand.bind(entry);

                        // Set BPM
                        await send({ type: "set_bpm", bpm: patch.bpm });

                        // Create full patch (same as generate_patch)
                        const kick = await send({ type: "add_module", module_type: "drum", params: { decay: 0.4, tone: 0.3 } });
                        const clap = await send({ type: "add_module", module_type: "drum", params: { decay: 0.12, tone: 0.85 } });
                        const hat = await send({ type: "add_module", module_type: "drum", params: { decay: 0.08, tone: 0.6 } });
                        const seqKick = await send({ type: "add_module", module_type: "seq" });
                        const seqClap = await send({ type: "add_module", module_type: "seq" });
                        const seqHat = await send({ type: "add_module", module_type: "seq" });
                        const bassOsc = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: -1 } });
                        const seqBass = await send({ type: "add_module", module_type: "seq" });
                        const padOsc1 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: 6 } });
                        const padOsc2 = await send({ type: "add_module", module_type: "osc", waveform: "sawtooth", params: { octave: 0, fine: -6 } });
                        const seqPad1 = await send({ type: "add_module", module_type: "seq" });
                        const seqPad2 = await send({ type: "add_module", module_type: "seq" });
                        const leadOsc = await send({ type: "add_module", module_type: "osc", waveform: "triangle", params: { octave: 1 } });
                        const seqLead = await send({ type: "add_module", module_type: "seq" });
                        const bassFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 500, resonance: 4 } });
                        const padFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 1200, resonance: 2 } });
                        const leadFilter = await send({ type: "add_module", module_type: "filter", params: { cutoff: 2500, resonance: 1.5 } });
                        const lfo = await send({ type: "add_module", module_type: "lfo", waveform: "sine", params: { rate: 0.2 } });
                        const chorus = await send({ type: "add_module", module_type: "chorus", params: { rate: 0.7, depth: 0.4, mix: 0.45 } });
                        const reverb = await send({ type: "add_module", module_type: "reverb", params: { decay: 3.5, mix: 0.5 } });
                        const delay = await send({ type: "add_module", module_type: "delay", params: { time: 0.375, feedback: 0.4, mix: 0.3 } });
                        const drumMixer = await send({ type: "add_module", module_type: "mixer" });
                        const padMixer = await send({ type: "add_module", module_type: "mixer" });
                        const compressor = await send({ type: "add_module", module_type: "compressor", params: { threshold: -10, ratio: 4, attack: 0.005, release: 0.15 } });

                        const id = (r) => r && r.module_id ? r.module_id : null;

                        // Program sequences
                        await send({ type: "set_sequence", module_id: id(seqKick), pattern: patch.drums.kick, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqClap), pattern: patch.drums.clap, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqHat), pattern: patch.drums.hat, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqBass), pattern: patch.bass, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqPad1), pattern: patch.pad, steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqPad2), pattern: patch.pad.map(s => ({ ...s, note: s.note ? s.note + 4 : s.note })), steps: 16 });
                        await send({ type: "set_sequence", module_id: id(seqLead), pattern: patch.lead, steps: 16 });

                        // Wire up
                        await send({ type: "connect", from_module: id(seqKick), from_jack: "gate_out", to_module: id(kick), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqClap), from_jack: "gate_out", to_module: id(clap), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqHat), from_jack: "gate_out", to_module: id(hat), to_jack: "gate" });
                        await send({ type: "connect", from_module: id(seqBass), from_jack: "cv_out", to_module: id(bassOsc), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqPad1), from_jack: "cv_out", to_module: id(padOsc1), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqPad2), from_jack: "cv_out", to_module: id(padOsc2), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(seqLead), from_jack: "cv_out", to_module: id(leadOsc), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(kick), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(clap), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(hat), from_jack: "audio_out", to_module: id(drumMixer), to_jack: "ch3" });
                        await send({ type: "connect", from_module: id(bassOsc), from_jack: "audio_out", to_module: id(bassFilter), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(padOsc1), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(padOsc2), from_jack: "audio_out", to_module: id(padMixer), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(padMixer), from_jack: "audio_out", to_module: id(padFilter), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(lfo), from_jack: "cv_out", to_module: id(padFilter), to_jack: "cv_in" });
                        await send({ type: "connect", from_module: id(padFilter), from_jack: "audio_out", to_module: id(chorus), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(chorus), from_jack: "audio_out", to_module: id(reverb), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(leadOsc), from_jack: "audio_out", to_module: id(leadFilter), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(leadFilter), from_jack: "audio_out", to_module: id(delay), to_jack: "audio_in" });
                        await send({ type: "connect", from_module: id(drumMixer), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch1" });
                        await send({ type: "connect", from_module: id(bassFilter), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch2" });
                        await send({ type: "connect", from_module: id(reverb), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch3" });
                        await send({ type: "connect", from_module: id(delay), from_jack: "audio_out", to_module: id(compressor), to_jack: "ch4" });

                        await send({ type: "play" });

                        return {
                            ok: true,
                            loaded_preset: {
                                id: preset.id,
                                name: preset.name,
                                description: preset.description,
                                author: preset.author,
                                genre: preset.genre,
                                bpm: preset.bpm,
                                key: preset.key,
                                scale: preset.scale,
                            }
                        };
                    },
                },
            ],
            open: async (ctx) => {
                let entry = servers.get(ctx.instanceId);
                if (!entry) {
                    entry = await startServer(ctx.instanceId);
                    servers.set(ctx.instanceId, entry);
                }
                return {
                    title: "SqncR",
                    url: entry.url,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    entry.wss.close();
                    await new Promise((resolve) => entry.server.close(() => resolve()));
                }
            },
        }),
    ],
});
