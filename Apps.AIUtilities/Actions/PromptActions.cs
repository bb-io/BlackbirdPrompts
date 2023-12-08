using System.Text;
using Apps.AIUtilities.Constants;
using Apps.AIUtilities.Enums;
using Apps.AIUtilities.Models.Request.Prompts;
using Apps.AIUtilities.Models.Response.Prompts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.AIUtilities.Actions;

[ActionList]
public class PromptActions
{
    private const string PromptSeparator = ";;";

    [Action("Summary prompt", Description = "Get prompt for summarizing text")]
    public PromptResponse Summary([ActionParameter] TextRequest input)
    {
        var promptText = BuildPromptFromInputs(input.Text, input.TextFile) ??
                         throw new("Both Text and File inputs can't be empty");

        return new(string.Format(Prompts.Summary, promptText));
    }

    [Action("Generate edit prompt", Description = "Get prompt for editing the input text given an instructions")]
    public PromptResponse GenerateEdit([ActionParameter] GenerateEditRequest input)
    {
        var promptText = BuildPromptFromInputs(input.Text, input.TextFile) ??
                         throw new("Both Text and File inputs can't be empty");

        var systemPrompt = Prompts.GenerateEditSystem;
        var userPrompt = string.Format(Prompts.GenerateEditUser, promptText, input.Instructions);

        return new(string.Join(PromptSeparator, systemPrompt, userPrompt));
    }

    [Action("Post-edit MT prompt",
        Description = "Get prompt for reviewing MT translated text and generating a post-edited version")]
    public PromptResponse PostEditMt([ActionParameter] PostEditMtRequest input)
    {
        var systemPrompt = input.AdditionalPrompt is null
            ? Prompts.PostEditMtSystem
            : $"{Prompts.PostEditMtSystem} {input.AdditionalPrompt}";

        var sourceTextPrompt = BuildPromptFromInputs(input.SourceText, input.SourceTextFile) ??
                               throw new("Both Source text and Source text file inputs can't be empty");

        var targetTextPrompt = BuildPromptFromInputs(input.TargetText, input.TargetTextFile) ??
                               throw new("Both Target text and Target text file inputs can't be empty");

        var userPrompt = string.Format(Prompts.TranslationReview, sourceTextPrompt, targetTextPrompt);
        return new(string.Join(PromptSeparator, systemPrompt, userPrompt));
    }

    [Action("Find translation issues prompt",
        Description = "Get prompt for reviewing text translation and generating a comment with the issue description")]
    public PromptResponse FindTranslationIssues([ActionParameter] TranslationRequest input)
    {
        var sourceLanguagePart = input.SourceLanguage != null ? $"written in {input.SourceLanguage} " : string.Empty;
        var targetLanguagePart = input.TargetLanguage != null ? $"written in {input.TargetLanguage}" : string.Empty;
        var systemPrompt = string.Format(Prompts.FindTranslationIssuesSystem, sourceLanguagePart, targetLanguagePart);

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var sourceTextPrompt = BuildPromptFromInputs(input.SourceText, input.SourceTextFile) ??
                               throw new("Both Source text and Source text file inputs can't be empty");

        var targetTextPrompt = BuildPromptFromInputs(input.TargetText, input.TargetTextFile) ??
                               throw new("Both Target text and Target text file inputs can't be empty");

        var userPrompt = string.Format(Prompts.TranslationReview, sourceTextPrompt, targetTextPrompt);
        return new(string.Join(PromptSeparator, systemPrompt, userPrompt));
    }

    [Action("MQM report prompt",
        Description =
            "Get prompt for performing an LQA Analysis of the translation. The result will be in the MQM framework form.")]
    public PromptResponse MqmReport([ActionParameter] MqmRequest input)
        => GetMqmPrompt(input, Prompts.MqmReportSystem);

    [Action("MQM dimension values prompt",
        Description =
            "Get prompt for performing an LQA Analysis of the translation. The result will be in the MQM framework form, namely the scores (between 1 and 10) of each dimension.")]
    public PromptResponse MqmDimensionValues([ActionParameter] MqmRequest input)
        => new($"{GetMqmPrompt(input, Prompts.MqmDimensionValuesSystem).Prompt}{PromptSeparator}{FileFormat.Json}");

    [Action("Translate prompt", Description = "Get prompt for localizing the provided text")]
    public PromptResponse Translate([ActionParameter] TranslateRequest input)
    {
        var textPrompt = BuildPromptFromInputs(input.Text, input.TextFile) ??
                         throw new("Both Text and Text file inputs can't be empty");

        return new(string.Format(Prompts.Translate, textPrompt, input.Locale));
    }

    [Action("Get localizable content from image prompt",
        Description = "Get prompt for retrieving localizable content from image")]
    public PromptResponse GetLocalizableContentFromImage()
        => new(Prompts.GetLocalizableContentFromImage);

    private string? BuildPromptFromInputs(string? text, File? textFile)
    {
        if (text is null && textFile is null)
            return null;

        var promptTextParts = new List<string>();

        if (text is not null)
            promptTextParts.Add(text);

        if (textFile is not null)
            promptTextParts.Add(Encoding.UTF8.GetString(textFile.Bytes));

        return string.Join(" ", promptTextParts);
    }

    private PromptResponse GetMqmPrompt(MqmRequest input, string systemPromptPart)
    {
        var systemPrompt = input.AdditionalPrompt is null
            ? systemPromptPart
            : $"{systemPromptPart} {input.AdditionalPrompt}";

        var sourceTextPrompt = BuildPromptFromInputs(input.SourceText, input.SourceTextFile) ??
                               throw new("Both Source text and Source text file inputs can't be empty");

        var targetTextPrompt = BuildPromptFromInputs(input.TargetText, input.TargetTextFile) ??
                               throw new("Both Target text and Target text file inputs can't be empty");

        var sourceLanguagePrompt = input.SourceLanguage != null ? $"The {input.SourceLanguage} " : string.Empty;
        var targetLanguagePrompt = input.TargetLanguage != null ? $" into {input.TargetLanguage}" : string.Empty;
        var targetAudiencePrompt = input.TargetAudience != null
            ? $" The target audience is {input.TargetAudience}"
            : string.Empty;

        var userPrompt = string.Format(Prompts.MqmUser, sourceLanguagePrompt, sourceTextPrompt, targetTextPrompt,
            targetLanguagePrompt, targetAudiencePrompt);

        return new(string.Join(PromptSeparator, systemPrompt, userPrompt));
    }
}