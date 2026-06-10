// Extension: modular-synth
// Modular software synthesizer with oscillators, sequencer, drums, virtual patching, MIDI, effects, polyphony, and presets
// Controllable programmatically via canvas actions for SqncR integration

import { createServer } from "node:http";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { WebSocketServer } from "ws";
import { joinSession, createCanvas } from "@github/copilot-sdk/extension";

const __dirname = dirname(fileURLToPath(import.meta.url));

const servers = new Map();

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
            case 'play': transport.play(); getAudioCtx(); return { ok: true };
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
        const html = getSynthHtml(0); // port 0 = same-origin WS
        res.setHeader("Content-Type", "text/html; charset=utf-8");
        res.end(html);
    });

    // WebSocket server attached to the HTTP server (same port, no CORS issues)
    const wss = new WebSocketServer({ server });

    wss.on("connection", (ws) => {
        clients.add(ws);
        ws.on("close", () => clients.delete(ws));
        ws.on("message", (data) => {
            try {
                const msg = JSON.parse(data);
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

    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
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
