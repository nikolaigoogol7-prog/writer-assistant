using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => "Writer API is running ✅");

app.MapPost("/humanize", (HumanizeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
        return Results.BadRequest(new { error = "Text is required." });

    var t = req.Text.Trim();

    // Normalize whitespace
    t = t.Replace("\r\n", "\n");
    while (t.Contains("  ")) t = t.Replace("  ", " ");
    t = t.Replace(" ,", ",").Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?");

    // Light de-stiffening swaps (meaning-preserving)
    t = ReplaceMany(t, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["in conclusion,"] = "to wrap it up,",
        ["furthermore,"] = "also,",
        ["therefore,"] = "so,",
        ["however,"] = "but,",
        ["utilize"] = "use",
        ["numerous"] = "many",
        ["individuals"] = "people",
        ["purchase"] = "buy",
        ["commence"] = "start",
        ["assist"] = "help",
        ["in order to"] = "to"
    });

    // Tone controls
    var tone = (req.Tone ?? "neutral").Trim().ToLowerInvariant();
    if (tone == "casual")
    {
        t = ReplaceMany(t, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["to wrap it up,"] = "overall,",
            ["additionally,"] = "plus,",
            ["moreover,"] = "also,"
        });
    }
    else if (tone == "formal")
    {
        t = ReplaceMany(t, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["so,"] = "therefore,",
            ["but,"] = "however,"
        });
    }

    // Break very long sentences (simple heuristic)
    if (req.BreakLongSentences)
    {
        t = SplitLongSentences(t, maxLen: 160);
    }

    // Optional contractions (English)
    if (req.Contractions)
    {
        t = ReplaceMany(t, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["do not"] = "don't",
            ["does not"] = "doesn't",
            ["did not"] = "didn't",
            ["cannot"] = "can't",
            ["will not"] = "won't",
            ["it is"] = "it's",
            ["that is"] = "that's",
            ["there is"] = "there's",
            ["we are"] = "we're",
            ["you are"] = "you're",
            ["they are"] = "they're"
        });
    }

    // Add a friendly header (optional)
    t = "Here’s a cleaner version:\n\n" + t;

    return Results.Ok(new { result = t });
});

app.Run();

static string ReplaceMany(string input, Dictionary<string, string> map)
{
    var output = input;

    // Replace longer keys first to avoid partial overlaps
    foreach (var kv in map.OrderByDescending(k => k.Key.Length))
    {
        output = ReplaceOrdinalIgnoreCase(output, kv.Key, kv.Value);
    }

    return output;
}

static string ReplaceOrdinalIgnoreCase(string input, string search, string replace)
{
    int index = 0;
    while (true)
    {
        index = input.IndexOf(search, index, StringComparison.OrdinalIgnoreCase);
        if (index < 0) break;

        input = input.Remove(index, search.Length).Insert(index, replace);
        index += replace.Length;
    }

    return input;
}

static string SplitLongSentences(string text, int maxLen)
{
    // Splits by ". " and then re-joins; if a sentence is too long, splits at ", " roughly in the middle.
    var parts = text.Split(new[] { ". " }, StringSplitOptions.None).ToList();

    for (int i = 0; i < parts.Count; i++)
    {
        var s = parts[i];
        if (s.Length <= maxLen) continue;

        var commaPositions = new List<int>();
        int idx = 0;
        while ((idx = s.IndexOf(", ", idx, StringComparison.Ordinal)) >= 0)
        {
            commaPositions.Add(idx);
            idx += 2;
        }

        if (commaPositions.Count > 0)
        {
            // Split near middle comma
            int mid = commaPositions[commaPositions.Count / 2];
            var left = s.Substring(0, mid).Trim();
            var right = s.Substring(mid + 2).Trim();

            parts[i] = left + ".";
            parts.Insert(i + 1, right);
            i++; // skip newly inserted
        }
    }

    return string.Join(". ", parts);
}

record HumanizeRequest(
    string Text,
    string? Tone = "neutral",      // neutral | casual | formal
    bool Contractions = true,      // true => "do not" -> "don't"
    bool BreakLongSentences = true // splits very long sentences
);
