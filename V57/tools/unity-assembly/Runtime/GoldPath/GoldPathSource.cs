using System.Collections.Generic;
using System.IO;

namespace V57.GoldPath
{
    /// <summary>
    /// Loads the gold path. Priority: the V57-authored <c>Docs/V57/gold_path.json</c> whenever it exists; otherwise
    /// <c>acceptance.json → gold_path</c>, but only if it is not a <c>status: "draft"</c> (intake always emits a draft,
    /// which must be authored into gold_path.json before it can gate anything — a draft alone yields an error).
    /// </summary>
    public static class GoldPathSource
    {
        #region Public Methods

        public static GoldPathDocument Load(string acceptancePath, string fallbackPath, List<string> errors)
        {
            if (File.Exists(fallbackPath))
            {
                GoldPathDocument authored = GoldPathStepParser.ParseGoldPathJson(File.ReadAllText(fallbackPath), fallbackPath, errors);
                if (!authored.HasSteps)
                {
                    errors.Add($"{fallbackPath}: no steps");
                }

                return authored;
            }

            if (!File.Exists(acceptancePath))
            {
                errors.Add($"{acceptancePath}: not found and {fallbackPath} absent — author the gold path (unity-gold-path-skill)");
                return new GoldPathDocument(acceptancePath, null, null);
            }

            List<string> acceptanceErrors = new List<string>();
            GoldPathDocument document = GoldPathStepParser.ParseAcceptanceJson(File.ReadAllText(acceptancePath), acceptancePath, acceptanceErrors);
            if (document.IsDraft)
            {
                errors.Add($"{acceptancePath}: gold_path is a draft — author {fallbackPath} (same schema) before running the gate");
                return new GoldPathDocument(acceptancePath, document.ScenePath, null, document.Status);
            }

            errors.AddRange(acceptanceErrors);
            if (!document.HasSteps && acceptanceErrors.Count == 0)
            {
                errors.Add($"{acceptancePath}: gold_path is null or has no steps and {fallbackPath} is absent");
            }

            return document;
        }

        #endregion
    }
}
