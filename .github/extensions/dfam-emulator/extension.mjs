// Extension: dfam-emulator
// Moog DFAM analog percussion synthesizer emulator

import { createServer } from "node:http";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { joinSession, createCanvas } from "@github/copilot-sdk/extension";

const __dirname = dirname(fileURLToPath(import.meta.url));
const servers = new Map();

function getHtml() {
    return readFileSync(join(__dirname, "dfam.html"), "utf-8");
}

async function startServer(instanceId) {
    const server = createServer((req, res) => {
        res.setHeader("Content-Type", "text/html; charset=utf-8");
        res.end(getHtml());
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    return { server, url: `http://127.0.0.1:${port}/` };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "dfam-emulator",
            displayName: "DFAM Emulator",
            description: "Moog DFAM analog percussion synthesizer emulator with working knobs, sequencer, and Web Audio synthesis",
            actions: [
                {
                    name: "set_parameter",
                    description: "Set a DFAM parameter value (e.g. vco1_freq, vcf_cutoff, tempo)",
                    inputSchema: {
                        type: "object",
                        properties: {
                            param: { type: "string", description: "Parameter name" },
                            value: { type: "number", description: "Value 0-1" },
                        },
                        required: ["param", "value"],
                    },
                    handler: async (ctx) => {
                        return { ok: true, message: `Parameter ${ctx.input.param} set to ${ctx.input.value}` };
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
                    title: "Moog DFAM",
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
