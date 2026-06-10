// Extension: modular-synth
// Modular software synthesizer with oscillators, sequencer, drums, virtual patching, MIDI, effects, polyphony, and presets

import { createServer } from "node:http";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { joinSession, createCanvas } from "@github/copilot-sdk/extension";

const __dirname = dirname(fileURLToPath(import.meta.url));
const synthHtml = readFileSync(join(__dirname, "synth.html"), "utf-8");

const servers = new Map();

async function startServer(instanceId) {
    const server = createServer((req, res) => {
        res.setHeader("Content-Type", "text/html; charset=utf-8");
        res.end(synthHtml);
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    return { server, url: `http://127.0.0.1:${port}/` };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "modular-synth",
            displayName: "Modular Synth",
            description:
                "A modular software synthesizer with oscillators, sequencer, drum voices, virtual patch cables, MIDI I/O with clock sync, effects (reverb, delay), polyphony, and preset save/load.",
            actions: [
                {
                    name: "add_module",
                    description: "Add a module to the synth rack",
                    inputSchema: {
                        type: "object",
                        properties: {
                            module_type: {
                                type: "string",
                                enum: ["osc", "seq", "drum", "env", "vca", "mixer", "reverb", "delay"],
                                description: "Type of module to add",
                            },
                        },
                        required: ["module_type"],
                    },
                    handler: async (ctx) => {
                        return { ok: true, message: `Add a ${ctx.input.module_type} module via the toolbar button in the synth UI` };
                    },
                },
                {
                    name: "get_status",
                    description: "Get the current synth status (loaded modules, transport state)",
                    handler: async (ctx) => {
                        return { ok: true, message: "Synth is running. Interact with the canvas UI to add modules and patch cables." };
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
                    title: "Modular Synth",
                    url: entry.url,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    await new Promise((resolve) => entry.server.close(() => resolve()));
                }
            },
        }),
    ],
});
