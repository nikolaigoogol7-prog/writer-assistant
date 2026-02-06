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

btn.addEventListener("click", async () => {
  const text = input.value.trim();
  if (!text) {
    setStatus("Paste some text first.");
    input.focus();
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
        text,
        tone: tone.value,
        contractions: contractions.checked,
        breakLongSentences: breakLong.checked
      })
    });

    const data = await res.json();

    if (!res.ok) {
      setStatus(data?.error || "Request failed.");
      return;
    }

 output.value = data.result ?? "";

output.classList.remove("fresh");
void output.offsetWidth; // force reflow
output.classList.add("fresh");
setTimeout(() => output.classList.remove("fresh"), 900);

copy.disabled = !output.value.trim();
setStatus("Done.");

  } catch (e) {
    setStatus("Could not reach the API. Is dotnet run running?");
  } finally {
    btn.disabled = false;
  }
});

copy.addEventListener("click", async () => {
  try {
    await navigator.clipboard.writeText(output.value);
    setStatus("Copied.");
  } catch {
    setStatus("Copy failed (browser permission).");
  }
});
