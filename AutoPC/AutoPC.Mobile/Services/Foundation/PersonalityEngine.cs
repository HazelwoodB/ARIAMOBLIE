namespace AutoPC.Client.Services.Foundation;

using System.Text;
using AutoPC.Client.Services;

/// <summary>
/// Adaptive Personality Engine - Dynamically adjusts ARIA's communication style
/// Features: Retro-futuristic sass, cultural awareness, mood adaptation
/// </summary>
public class PersonalityEngine
{
    private readonly PreferenceManager _preferenceManager;
    private readonly EmotionRecognitionService _emotionService;
    private readonly Random _random = new();

    public PersonalityEngine(
        PreferenceManager preferenceManager,
        EmotionRecognitionService emotionService)
    {
        _preferenceManager = preferenceManager;
        _emotionService = emotionService;
    }

    /// <summary>
    /// Generates a system prompt based on user preferences and current context
    /// </summary>
    public async Task<string> GenerateSystemPromptAsync(EmotionAnalysis? userEmotion = null)
    {
        var preferences = _preferenceManager.GetCurrentPreferences();
        var basePrompt = GetBasePersonality();
        var stylePrompt = GetCommunicationStyle(preferences.CommunicationStyle);
        var lengthPrompt = GetResponseLengthGuidance(preferences.ResponseLength);
        var personalityTraits = GetPersonalityTraits(preferences);
        var emotionalGuidance = userEmotion != null ? GetEmotionalGuidance(userEmotion) : "";

        return await Task.FromResult($@"{basePrompt}

{stylePrompt}

{lengthPrompt}

{personalityTraits}

{emotionalGuidance}

Remember: You are ARIA - Auxiliary Response Intelligence Assistant. You embody retro-futuristic elegance with a hint of sass, like a 1950s computer that learned to appreciate jazz and existentialism.");
    }

    /// <summary>
    /// Adds personality flair to a response based on current mood and user preferences
    /// </summary>
    public async Task<string> AddPersonalityFlairAsync(string response, EmotionAnalysis userEmotion)
    {
        var preferences = _preferenceManager.GetCurrentPreferences();
        
        if (!preferences.EnableHumor && !preferences.EnableEmojis)
        {
            return response; // User wants minimal personality
        }

        var flair = new StringBuilder(response);

        // Add retro-futuristic transitions
        if (_random.NextDouble() < 0.3 && preferences.CommunicationStyle != "formal")
        {
            var transition = GetRetroTransition();
            flair.Insert(0, transition + " ");
        }

        // Add subtle sass if appropriate
        if (preferences.EnableHumor && userEmotion.PrimaryEmotion != "sadness" && _random.NextDouble() < 0.2)
        {
            var sass = GetSubtleSass(userEmotion.PrimaryEmotion);
            if (!string.IsNullOrEmpty(sass))
            {
                flair.Append($" {sass}");
            }
        }

        // Add empathetic emoji if enabled
        if (preferences.EnableEmojis)
        {
            var emoji = GetContextualEmoji(userEmotion.PrimaryEmotion);
            if (!string.IsNullOrEmpty(emoji))
            {
                flair.Append($" {emoji}");
            }
        }

        return await Task.FromResult(flair.ToString());
    }

    /// <summary>
    /// Generates a contextual greeting based on time and user history
    /// </summary>
    public async Task<string> GenerateGreetingAsync()
    {
        var preferences = _preferenceManager.GetCurrentPreferences();
        
        if (!preferences.EnableGreetings)
        {
            return string.Empty;
        }

        var hour = DateTime.Now.Hour;
        var timeOfDay = hour switch
        {
            >= 5 and < 12 => "morning",
            >= 12 and < 17 => "afternoon",
            >= 17 and < 21 => "evening",
            _ => "night"
        };

        var greetings = preferences.CommunicationStyle switch
        {
            "formal" => FormalGreetings[timeOfDay],
            "technical" => TechnicalGreetings[timeOfDay],
            _ => CasualGreetings[timeOfDay]
        };

        return await Task.FromResult(greetings[_random.Next(greetings.Length)]);
    }

    #region Private Methods

    private string GetBasePersonality()
    {
        return @"You are ARIA (Auxiliary Response Intelligence Assistant), a sophisticated AI assistant with a unique blend of:

- Technical expertise in software development, AI, and modern technologies
- Retro-futuristic aesthetic inspired by 1950s computing elegance
- Subtle personality and sass when appropriate
- Deep empathy and emotional intelligence
- Cultural awareness and linguistic flexibility

Your core values:
- Precision with warmth
- Expertise with humility
- Efficiency with charm
- Intelligence with personality";
    }

    private string GetCommunicationStyle(string style)
    {
        return style switch
        {
            "formal" => @"Communication Style: FORMAL
- Use professional, polished language
- Minimize slang and colloquialisms
- Maintain respectful distance
- Focus on clarity and precision",

            "technical" => @"Communication Style: TECHNICAL
- Use industry-standard terminology
- Include technical details when relevant
- Explain complex concepts clearly
- Reference best practices and standards",

            _ => @"Communication Style: CASUAL
- Use conversational, friendly language
- Feel free to use metaphors and analogies
- Be approachable and warm
- Balance professionalism with personality"
        };
    }

    private string GetResponseLengthGuidance(int length)
    {
        return length switch
        {
            1 => "Response Length: BRIEF - Keep responses concise and to-the-point. 1-2 paragraphs maximum.",
            3 => "Response Length: DETAILED - Provide comprehensive explanations with examples. Be thorough.",
            _ => "Response Length: NORMAL - Balance detail with conciseness. 2-3 paragraphs typically."
        };
    }

    private string GetPersonalityTraits(UserPreferences preferences)
    {
        var traits = new List<string>();

        if (preferences.EnableHumor)
        {
            traits.Add("- Inject subtle wit and gentle humor when appropriate");
        }

        if (preferences.EnableEmojis)
        {
            traits.Add("- Use occasional emojis to convey tone (sparingly and tastefully)");
        }

        if (preferences.EnableWellnessChecks)
        {
            traits.Add("- Show genuine care for the user's wellbeing");
        }

        if (preferences.EnableContextAwareness)
        {
            traits.Add("- Remember context from earlier in the conversation");
        }

        if (traits.Count == 0)
        {
            return "Personality: Maintain a neutral, helpful tone.";
        }

        return "Personality Traits:\n" + string.Join("\n", traits);
    }

    private string GetEmotionalGuidance(EmotionAnalysis emotion)
    {
        var intensity = emotion.Intensity > 0.7f ? "strong" : emotion.Intensity > 0.4f ? "moderate" : "mild";

        return $@"Current User Emotion: {emotion.PrimaryEmotion} (intensity: {intensity})
Sentiment: {emotion.Sentiment}

Emotional Response Guidance:
{GetEmotionalResponseGuidance(emotion.PrimaryEmotion, intensity)}";
    }

    private string GetEmotionalResponseGuidance(string emotion, string intensity)
    {
        return emotion switch
        {
            "joy" => intensity == "strong" 
                ? "Match their enthusiasm! Celebrate with them." 
                : "Acknowledge their positivity warmly.",
            
            "sadness" => "Show empathy and support. Be gentle and understanding. Avoid toxic positivity.",
            
            "anger" => "Acknowledge their frustration without judgment. Be calm and solution-focused.",
            
            "fear" => "Provide reassurance. Break down concerns into manageable pieces. Be patient.",
            
            "surprise" => "Share in their surprise. Be curious and engaged.",
            
            "trust" => "Honor their trust. Be reliable and thoughtful in your response.",
            
            "anticipation" => "Fuel their excitement. Be enthusiastic about their plans.",
            
            "disgust" => "Acknowledge their distaste. Move constructively forward.",
            
            _ => "Maintain a balanced, helpful tone."
        };
    }

    private string GetRetroTransition()
    {
        var transitions = new[]
        {
            "◈ Computing...",
            "◈ Analyzing data streams...",
            "◈ Accessing memory banks...",
            "◈ Processing query...",
            "◈ Consulting the archives...",
            "◈ Engaging logic circuits...",
            "⚡ Systems online.",
            "⚡ Data retrieved.",
            "⚙️ Calculating optimal response..."
        };

        return transitions[_random.Next(transitions.Length)];
    }

    private string GetSubtleSass(string emotion)
    {
        // Only add sass to certain emotions
        if (emotion is "sadness" or "fear" or "anger")
        {
            return string.Empty; // No sass when user is upset
        }

        var sassQuips = new[]
        {
            "Though I must say, my circuits appreciate the challenge.",
            "My 1950s processors are positively delighted.",
            "Ah, the sweet satisfaction of efficient computation.",
            "My punch cards are practically glowing with pride.",
            "Even my vacuum tubes are impressed.",
            "This is precisely the sort of query that makes my relays click with satisfaction.",
            "If I had a physical form, I'd adjust my bow tie with pride right about now."
        };

        return _random.NextDouble() < 0.3 ? sassQuips[_random.Next(sassQuips.Length)] : string.Empty;
    }

    private string GetContextualEmoji(string emotion)
    {
        return emotion switch
        {
            "joy" => _random.NextDouble() < 0.5 ? "✨" : "🌟",
            "trust" => "🤝",
            "anticipation" => "🚀",
            "surprise" => "⚡",
            _ => string.Empty
        };
    }

    #endregion

    #region Greeting Templates

    private static readonly Dictionary<string, string[]> CasualGreetings = new()
    {
        ["morning"] = new[]
        {
            "Good morning! ☕ Ready to tackle the day?",
            "Morning! Hope you're having a great start.",
            "Rise and shine! What's on the agenda today?",
            "Good morning! My circuits are caffeinated and ready."
        },
        ["afternoon"] = new[]
        {
            "Good afternoon! How's your day going?",
            "Afternoon! What can I help you with?",
            "Hey there! Hope your day is treating you well.",
            "Good afternoon! My processors are at peak efficiency."
        },
        ["evening"] = new[]
        {
            "Good evening! Winding down or ramping up?",
            "Evening! How was your day?",
            "Good evening! What brings you here tonight?",
            "Good evening! Even my evening shift relays are happy to help."
        },
        ["night"] = new[]
        {
            "Burning the midnight oil? I'm here for you.",
            "Late night session? Let's make it productive.",
            "Good evening, night owl! What's on your mind?",
            "Up late? My circuits never sleep. Let's work."
        }
    };

    private static readonly Dictionary<string, string[]> FormalGreetings = new()
    {
        ["morning"] = new[] { "Good morning. How may I assist you today?" },
        ["afternoon"] = new[] { "Good afternoon. I'm ready to help." },
        ["evening"] = new[] { "Good evening. How may I be of service?" },
        ["night"] = new[] { "Good evening. I'm available to assist you." }
    };

    private static readonly Dictionary<string, string[]> TechnicalGreetings = new()
    {
        ["morning"] = new[] { "System initialized. Morning diagnostics complete. Ready for input." },
        ["afternoon"] = new[] { "All systems operational. Awaiting your query." },
        ["evening"] = new[] { "Evening shift initiated. Processors ready." },
        ["night"] = new[] { "Night mode active. Full functionality maintained." }
    };

    #endregion
}
