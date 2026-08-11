// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    // Note: Also includes Ollama model info to avoid having to handle
    // renamed files in unitypackage.

    public enum ModelType { Completion, Chat, Edit, Transcription, Reasoning }

    public class Model
    {
        public string Name { get; set; }
        public ModelType ModelType { get; private set; }
        public int MaxTokens { get; private set; }

        public Model(string name, ModelType modelType, int maxTokens)
        {
            Name = name;
            ModelType = modelType;
            MaxTokens = maxTokens;
        }

        public const TextModelName DefaultTextModelName = TextModelName.GPT5_6_Luna;

        public bool IsOllamaModel => this == Llama3_2 || this == Llama3_3 || this == CustomOllamaModel;

        public string ServiceName => IsOllamaModel ? "Ollama" : "OpenAI";

        public const string ImageGenerationModel = "gpt-image-2";

        /// <summary>
        /// Llama 3.2 model.
        /// </summary>
        public static Model Llama3_2 { get; } = new Model("llama3.2:latest", ModelType.Chat, 128000);

        /// <summary>
        /// Llama 3.3 model.
        /// </summary>
        public static Model Llama3_3 { get; } = new Model("llama3.3", ModelType.Chat, 128000);

        /// <summary>
        /// Arbitrary Ollama-managed model.
        /// </summary>
        public static Model CustomOllamaModel { get; } = new Model("llama3.3", ModelType.Chat, 128000);

        //---

        /// <summary>
        /// Frontier model for complex professional work.
        /// </summary>
        public static Model GPT5_6_Sol { get; } = new Model("gpt-5.6-sol", ModelType.Reasoning, 1050000);

        /// <summary>
        /// GPT-5.6 model that balances intelligence and cost.
        /// </summary>
        public static Model GPT5_6_Terra { get; } = new Model("gpt-5.6-terra", ModelType.Reasoning, 1050000);

        /// <summary>
        /// GPT-5.6 model optimized for cost-sensitive workloads.
        /// </summary>
        public static Model GPT5_6_Luna { get; } = new Model("gpt-5.6-luna", ModelType.Reasoning, 1050000);

        /// <summary>
        /// A new class of intelligence for coding and professional work.
        /// </summary>
        public static Model GPT5_5 { get; } = new Model("gpt-5.5", ModelType.Reasoning, 1050000);

        /// <summary>
        /// A new class of intelligence for coding and professional work.
        /// </summary>
        public static Model GPT5_5_Pro { get; } = new Model("gpt-5.5-pro", ModelType.Reasoning, 1050000);

        /// <summary>
        /// Best intelligence at scale for agentic, coding, and professional workflows.
        /// </summary>
        public static Model GPT5_4 { get; } = new Model("gpt-5.4", ModelType.Reasoning, 1050000);

        /// <summary>
        /// Best intelligence at scale for agentic, coding, and professional workflows.
        /// </summary>
        public static Model GPT5_4_Pro { get; } = new Model("gpt-5.4-pro", ModelType.Reasoning, 1050000);

        /// <summary>
        /// Strongest mini model yet for coding, computer use, and subagents.
        /// </summary>
        public static Model GPT5_4_mini { get; } = new Model("gpt-5.4-mini", ModelType.Reasoning, 400000);

        /// <summary>
        /// Cheapest GPT-5.4-class model for simple high-volume tasks.
        /// </summary>
        public static Model GPT5_4_nano { get; } = new Model("gpt-5.4-nano", ModelType.Reasoning, 400000);

        /// <summary>
        /// Complex reasoning, broad world knowledge, and code-heavy or multi-step agentic tasks
        /// </summary>
        public static Model GPT5_2 { get; } = new Model("gpt-5.2", ModelType.Reasoning, 400000);

        /// <summary>
        /// Complex reasoning, broad world knowledge, and code-heavy or multi-step agentic tasks
        /// </summary>
        public static Model GPT5_2_Pro { get; } = new Model("gpt-5.2-pro", ModelType.Reasoning, 400000);

        /// <summary>
        /// Complex reasoning, broad world knowledge, and code-heavy or multi-step agentic tasks
        /// </summary>
        public static Model GPT5_1 { get; } = new Model("gpt-5.1", ModelType.Reasoning, 400000);

        /// <summary>
        /// Flagship model for coding, reasoning, and agentic tasks across domains.
        /// </summary>
        public static Model GPT5 { get; } = new Model("gpt-5", ModelType.Reasoning, 400000);

        /// <summary>
        /// Faster, more cost-efficient version of GPT-5. 
        /// </summary>
        public static Model GPT5_mini { get; } = new Model("gpt-5-mini", ModelType.Reasoning, 400000);

        /// <summary>
        /// Fastest, cheapest version of GPT-5.
        /// </summary>
        public static Model GPT5_nano { get; } = new Model("gpt-5-nano", ModelType.Reasoning, 400000);

        /// <summary>
        /// Smartest non-reasoning model.
        /// </summary>
        public static Model GPT4_1 { get; } = new Model("gpt-4.1", ModelType.Chat, 1047576);

        ///// <summary>
        ///// Smaller, faster version of GPT-4.1.
        ///// </summary>
        //public static Model GPT4_1_mini { get; } = new Model("gpt-4.1-mini", ModelType.Chat, 1047576);

        ///// <summary>
        ///// Fastest, most cost-efficient version of GPT-4.1.
        ///// </summary>
        //public static Model GPT4_1_nano { get; } = new Model("gpt-4.1-nano", ModelType.Chat, 1047576);

        ///// <summary>
        ///// Our affordable and intelligent small model for fast, lightweight tasks.
        ///// </summary>
        //public static Model GPT4o_mini { get; } = new Model("gpt-4o-mini", ModelType.Chat, 128000);

        ///// <summary>
        ///// The latest GPT-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens.
        ///// </summary>
        //public static Model GPT4o { get; } = new Model("gpt-4o", ModelType.Chat, 128000);

        /// <summary>
        /// Fast, affordable high-intelligence reasoning model.
        /// </summary>
        public static Model o4_mini { get; } = new Model("o4-mini", ModelType.Reasoning, 200000);

        /// <summary>
        /// OpenAI's most powerful reasoning model.
        /// </summary>
        public static Model o3 { get; } = new Model("o3", ModelType.Reasoning, 200000);

        /// <summary>
        /// Fast, flexible, intelligent reasoning model.
        /// </summary>
        public static Model o3_mini { get; } = new Model("o3-mini", ModelType.Reasoning, 200000);

        /// <summary>
        /// A faster, more affordable reasoning model than o1.
        /// </summary>
        public static Model o1_mini { get; } = new Model("o1-mini", ModelType.Reasoning, 200000);

        /// <summary>
        /// High-intelligence reasoning model.
        /// </summary>
        public static Model o1 { get; } = new Model("o1", ModelType.Reasoning, 200000);

        /// <summary>
        /// A version of o1 with more compute for better responses.
        /// </summary>
        public static Model o1_pro { get; } = new Model("o1-pro", ModelType.Reasoning, 200000);

        ///// <summary>
        ///// The latest GPT-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. Returns a maximum of 4,096 output tokens.
        ///// </summary>
        //public static Model GPT4_Turbo { get; } = new Model("gpt-4-turbo", ModelType.Chat, 128000);

        ///// <summary>
        ///// More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat.
        ///// </summary>
        //public static Model GPT4 { get; } = new Model("gpt-4", ModelType.Chat, 8192);

        ///// <summary>
        ///// Same capabilities as the base gpt-4 mode but with 4x the context length. Will be updated with our latest model iteration.
        ///// </summary>
        //public static Model GPT4_32K { get; } = new Model("gpt-4-32k", ModelType.Chat, 32768);

        ///// <summary>
        ///// Most capable GPT-3.5 model and optimized for chat at 1/10th the cost of text-davinci-003. Will be updated with our latest model iteration.
        ///// </summary>
        //public static Model GPT3_5_Turbo { get; } = new Model("gpt-3.5-turbo", ModelType.Chat, 4096);

        ///// <summary>
        ///// Like GPT-3.5 Turbo but with 4x the context.
        ///// </summary>
        //public static Model GPT3_5_Turbo_16K { get; } = new Model("gpt-3.5-turbo-16k", ModelType.Chat, 16384);

        ///// <summary>
        ///// Can do any language task with better quality, longer output, and consistent instruction-following than the curie, babbage, or ada models. Also supports inserting completions within text.
        ///// </summary>
        //public static Model Davinci_003 { get; } = new Model("text-davinci-003", ModelType.Completion, 4097);

        ///// <summary>
        ///// Used for Edits.
        ///// </summary>
        //public static Model Davinci_Edit_001 { get; } = new Model("text-davinci-edit-001", ModelType.Edit, 4097);

        ///// <summary>
        ///// Very capable, faster and lower cost than Davinci.
        ///// </summary>
        //public static Model Curie { get; } = new Model("text-curie-001", ModelType.Completion, 2049);

        ///// <summary>
        ///// Capable of straightforward tasks, very fast, and lower cost.
        ///// </summary>
        //public static Model Babbage { get; } = new Model("text-babbage-001", ModelType.Completion, 2049);

        ///// <summary>
        ///// Capable of very simple tasks, usually the fastest model in the GPT-3 series, and lowest cost.
        ///// </summary>
        //public static Model Ada { get; } = new Model("text-ada-001", ModelType.Completion, 2049);

        /// <summary>
        /// Model for speech to text transcription.
        /// </summary>
        public static Model Whisper_1 { get; } = new Model("whisper-1", ModelType.Transcription, 65536);
    }

}

#endif
