const $ = (id) => document.getElementById(id);

const input = $("input");
const output = $("output");
const tone = $("tone");
const contractions = $("contractions");
const breakLong = $("breakLong");
const btn = $("btn");
const copy = $("copy");
const status = $("status");

function setStatus(msg) {
  status.textContent = msg || "";
}

function setOutputText(text) {
  if (output && "value" in output) output.value = text;
  else if (output) output.textContent = text;
}

function getOutputText() {
  if (!output) return "";
  if ("value" in output) return (output.value || "").trim();
  return (output.textContent || "").trim();
}

btn.addEventListener("click", async () => {
  const text = (input?.value || "").trim();
  if (!text) {
    setStatus("Paste some text first.");
    input?.focus();
    return;
  }

  btn.disabled = true;
  copy.disabled = true;
  setStatus("Working…");

  try {
    const res = await fetch("/humanize", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        Text: text,
        Tone: tone?.value || "neutral",
        Contractions: !!contractions?.checked,
        BreakLongSentences: !!breakLong?.checked
      })
    });

    const data = await res.json().catch(() => ({}));

    if (!res.ok) {
      setStatus(data?.error || `Request failed (${res.status}).`);
      setOutputText("");
      return;
    }

    const textOut = data.text ?? data.result ?? "";
    setOutputText(textOut);

    // animation class
    output?.classList.remove("fresh");
    void output?.offsetWidth; // reflow
    output?.classList.add("fresh");
    setTimeout(() => output?.classList.remove("fresh"), 900);

    copy.disabled = !getOutputText();
    setStatus("Done.");
  } catch (e) {
    console.error(e);
    setStatus("Could not reach the API.");
    setOutputText("");
  } finally {
    btn.disabled = false;
  }
});

copy.addEventListener("click", async () => {
  const textToCopy = getOutputText();
  if (!textToCopy) {
    setStatus("Nothing to copy yet.");
    return;
  }

  try {
    await navigator.clipboard.writeText(textToCopy);
    setStatus("Copied.");
  } catch {
    setStatus("Copy failed (browser permission).");
  }
});
