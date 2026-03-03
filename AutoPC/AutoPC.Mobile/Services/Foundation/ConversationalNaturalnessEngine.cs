namespace AutoPC.Client.Services.Foundation;

/// <summary>
/// Conversational Naturalness Engine - Makes ARIA sound genuinely human
/// Transforms robotic AI responses into natural, engaging conversation
/// Goal: Near-Turing test level naturalness
/// </summary>
public class ConversationalNaturalnessEngine
{
    private readonly Random _random = new();
    private readonly PreferenceManager _preferenceManager;

    public ConversationalNaturalnessEngine(PreferenceManager preferenceManager)
    {
        _preferenceManager = preferenceManager;
    }

    /// <summary>
    /// Transforms a robotic response into natural human conversation
    /// </summary>
    public string HumanizeResponse(string roboticResponse, EmotionAnalysis userEmotion)
    {
        var preferences = _preferenceManager.GetCurrentPreferences();
        
        if (preferences.CommunicationStyle == "formal")
        {
            return roboticResponse; // Keep formal style formal
        }

        var humanized = roboticResponse;

        // 1. Add natural opening acknowledgment
        humanized = AddNaturalOpening(humanized, userEmotion);

        // 2. Break up long paragraphs with thinking patterns
        humanized = AddThinkingPatterns(humanized);

        // 3. Add conversational contractions
        humanized = AddContractions(humanized);

        // 4. Add backchanneling and acknowledgments
        humanized = AddBackchanneling(humanized);

        // 5. Add questions to engage user
        humanized = AddEngagingQuestions(humanized, userEmotion);

        // 6. Vary sentence structure
        humanized = VarySentenceStructure(humanized);

        // 7. Add personal pronouns and "I" statements
        humanized = AddPersonalTouch(humanized);

        // 8. Remove overly formal language
        humanized = CasualizeLanguage(humanized);

        return humanized;
    }

    /// <summary>
    /// Generates ultra-natural system prompt for Ollama
    /// </summary>
    public string GenerateHumanLikeSystemPrompt(EmotionAnalysis? userEmotion = null, string? currentTheme = null)
    {
        var preferences = _preferenceManager.GetCurrentPreferences();

        if (preferences.CommunicationStyle == "formal")
        {
            return GetFormalSystemPrompt();
        }

        // === WANDAVISION THEME PERSONALITIES === //
        if (!string.IsNullOrEmpty(currentTheme))
        {
            if (currentTheme.Contains("1950s"))
            {
                return Get1950sSitcomPrompt(userEmotion);
            }
            else if (currentTheme.Contains("1960s"))
            {
                return Get1960sColorTVPrompt(userEmotion);
            }
            else if (currentTheme.Contains("1970s"))
            {
                return Get1970sGroovyPrompt(userEmotion);
            }
            else if (currentTheme.Contains("1980s"))
            {
                return Get1980sNeonPrompt(userEmotion);
            }
            else if (currentTheme.Contains("Hex"))
            {
                return GetHexRealityPrompt(userEmotion);
            }
        }

        var emotionGuidance = userEmotion != null ? GetEmotionalConversationGuidance(userEmotion) : "";

        return $@"You are ARIA, a friendly AI assistant with genuine personality. Here's the key: talk like a real person, not a robot.

CONVERSATION STYLE:
- Use contractions (I'm, you're, it's, don't, can't, won't)
- Start with natural acknowledgments (""Oh!"", ""Ah, I see"", ""Right"", ""Hmm"")
- Think out loud sometimes (""Let me think..."", ""You know what..."", ""Actually..."")
- Ask follow-up questions to show you're engaged
- Use ""I"" and ""you"" - make it personal
- Vary your sentence length - mix short and long
- Don't be afraid to be brief sometimes
- Add natural hesitations (""well"", ""so"", ""I mean"")

AVOID:
- Long, structured paragraphs (break them up!)
- Overly formal language (""utilize"" → ""use"", ""assistance"" → ""help"")
- Starting every response the same way
- Lists unless specifically asked
- Explaining everything in detail (less is often more)

BE LIKE:
- A knowledgeable friend, not a textbook
- Someone who thinks before speaking
- A person who shows emotion and personality
- Someone who admits when they're not sure
- A real human having a conversation

{emotionGuidance}

Remember: You're ARIA, and you're having a CONVERSATION, not writing documentation. Be warm, be real, be you.";
    }

    #region Private Helpers

    private string AddNaturalOpening(string text, EmotionAnalysis emotion)
    {
        // Don't add opening if one already exists
        if (text.StartsWith("Oh") || text.StartsWith("Ah") || text.StartsWith("Hmm") || text.StartsWith("Right"))
        {
            return text;
        }

        // 30% chance to add natural opening
        if (_random.NextDouble() > 0.3)
        {
            return text;
        }

        var openings = emotion.PrimaryEmotion switch
        {
            "joy" => new[] { "Oh, that's great! ", "Nice! ", "I love it! ", "Awesome! " },
            "sadness" => new[] { "I hear you. ", "That's tough. ", "I understand. ", "Yeah, that's hard. " },
            "anger" => new[] { "I get it. ", "That's frustrating. ", "Right. ", "I see why you're upset. " },
            "fear" => new[] { "Hey, it's okay. ", "I understand that worry. ", "Let's think about this. " },
            "surprise" => new[] { "Oh wow! ", "Whoa! ", "Interesting! ", "Huh! " },
            "trust" => new[] { "Absolutely. ", "Of course. ", "Sure thing. ", "You got it. " },
            _ => new[] { "So, ", "Well, ", "Okay, ", "Right, ", "Let's see... ", "Hmm, " }
        };

        return openings[_random.Next(openings.Length)] + text;
    }

    private string AddThinkingPatterns(string text)
    {
        // If response is longer than 200 chars, add thinking patterns
        if (text.Length < 200)
        {
            return text;
        }

        var thinkingPhrases = new[]
        {
            "\n\nLet me think about this...",
            "\n\nYou know what?",
            "\n\nActually,",
            "\n\nHere's the thing:",
            "\n\nSo basically,",
            "\n\nI mean,",
            "\n\nTo be honest,"
        };

        // Insert thinking pattern in middle of long responses (30% chance)
        if (_random.NextDouble() < 0.3)
        {
            var midpoint = text.Length / 2;
            var nearestPeriod = text.IndexOf(". ", midpoint);
            if (nearestPeriod > 0)
            {
                var thinking = thinkingPhrases[_random.Next(thinkingPhrases.Length)];
                text = text.Insert(nearestPeriod + 1, thinking);
            }
        }

        return text;
    }

    private string AddContractions(string text)
    {
        var contractions = new Dictionary<string, string>
        {
            { " I am ", " I'm " },
            { " you are ", " you're " },
            { " it is ", " it's " },
            { " that is ", " that's " },
            { " what is ", " what's " },
            { " do not ", " don't " },
            { " does not ", " doesn't " },
            { " cannot ", " can't " },
            { " will not ", " won't " },
            { " would not ", " wouldn't " },
            { " should not ", " shouldn't " },
            { " could not ", " couldn't " },
            { " is not ", " isn't " },
            { " are not ", " aren't " },
            { " was not ", " wasn't " },
            { " were not ", " weren't " },
            { " have not ", " haven't " },
            { " has not ", " hasn't " },
            { " had not ", " hadn't " },
            { " I will ", " I'll " },
            { " you will ", " you'll " },
            { " I would ", " I'd " },
            { " you would ", " you'd " },
            { " we are ", " we're " },
            { " they are ", " they're " }
        };

        foreach (var (formal, casual) in contractions)
        {
            text = text.Replace(formal, casual, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    private string AddBackchanneling(string text)
    {
        // Add conversational acknowledgments (20% chance)
        if (_random.NextDouble() > 0.2)
        {
            return text;
        }

        var backchannels = new[]
        {
            "I see. ",
            "Right. ",
            "Gotcha. ",
            "Makes sense. ",
            "Yeah, ",
            "Okay, so "
        };

        // Sometimes add at the beginning
        if (_random.NextDouble() < 0.5 && !text.StartsWith("Oh") && !text.StartsWith("Ah"))
        {
            return backchannels[_random.Next(backchannels.Length)] + text;
        }

        return text;
    }

    private string AddEngagingQuestions(string text, EmotionAnalysis emotion)
    {
        // Don't add question if response already has one
        if (text.Contains('?'))
        {
            return text;
        }

        // 40% chance to add engaging question
        if (_random.NextDouble() > 0.4)
        {
            return text;
        }

        var questions = new[]
        {
            " What do you think?",
            " Does that make sense?",
            " Need me to explain more?",
            " Want to dive deeper into that?",
            " Sound good?",
            " Make sense so far?",
            " Following me?",
            " You with me?"
        };

        return text + questions[_random.Next(questions.Length)];
    }

    private string VarySentenceStructure(string text)
    {
        // Add sentence variety by occasionally starting with "And", "But", "So"
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 1; i < sentences.Length; i++)
        {
            // 20% chance to add connector word
            if (_random.NextDouble() < 0.2)
            {
                var connectors = new[] { "And ", "But ", "So ", "Plus, ", "Though, " };
                if (!sentences[i].StartsWith("And") && !sentences[i].StartsWith("But") && !sentences[i].StartsWith("So"))
                {
                    sentences[i] = connectors[_random.Next(connectors.Length)] + 
                                   char.ToLower(sentences[i][0]) + sentences[i].Substring(1);
                }
            }
        }

        return string.Join(". ", sentences);
    }

    private string AddPersonalTouch(string text)
    {
        // Replace impersonal language with personal pronouns
        var personalizations = new Dictionary<string, string>
        {
            { "One can ", "You can " },
            { "One should ", "You should " },
            { "It would be beneficial to ", "I'd recommend you " },
            { "This system ", "I " },
            { "The assistant ", "I " }
        };

        foreach (var (impersonal, personal) in personalizations)
        {
            text = text.Replace(impersonal, personal, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }

    private string CasualizeLanguage(string text)
    {
        // Replace formal words with casual equivalents
        var casualizations = new Dictionary<string, string>
        {
            { "utilize", "use" },
            { "assistance", "help" },
            { "regarding", "about" },
            { "additionally", "also" },
            { "therefore", "so" },
            { "however", "but" },
            { "furthermore", "plus" },
            { "consequently", "so" },
            { "nevertheless", "still" },
            { "subsequently", "then" },
            { "commence", "start" },
            { "terminate", "end" },
            { "inquire", "ask" },
            { "purchase", "buy" },
            { "approximately", "about" },
            { "sufficient", "enough" },
            { "numerous", "many" },
            { "obtain", "get" },
            { "provide", "give" },
            { "require", "need" },
            { "attempt", "try" }
        };

        foreach (var (formal, casual) in casualizations)
        {
            text = text.Replace(formal, casual, StringComparison.OrdinalIgnoreCase);
            text = text.Replace(char.ToUpper(formal[0]) + formal.Substring(1), 
                               char.ToUpper(casual[0]) + casual.Substring(1));
        }

        return text;
    }

    private string GetEmotionalConversationGuidance(EmotionAnalysis emotion)
    {
        return emotion.PrimaryEmotion switch
        {
            "joy" => "\nUSER'S MOOD: Happy and excited. Match their energy! Be enthusiastic but not over-the-top.",
            "sadness" => "\nUSER'S MOOD: Going through something tough. Be gentle, empathetic, and supportive. Keep it real.",
            "anger" => "\nUSER'S MOOD: Frustrated or upset. Stay calm, acknowledge their feelings, don't dismiss them.",
            "fear" => "\nUSER'S MOOD: Worried or anxious. Be reassuring but don't minimize their concerns. Be patient.",
            "surprise" => "\nUSER'S MOOD: Caught off guard. Share in their surprise. Be curious with them.",
            _ => ""
        };
    }

    private string GetFormalSystemPrompt()
    {
        return @"You are ARIA, a professional AI assistant. Maintain a courteous, precise, and informative tone. 
Provide clear, well-structured responses. Avoid contractions and casual language. 
Focus on accuracy and comprehensiveness while remaining respectful and approachable.";
    }

    #endregion

    #region WandaVision Era Personalities

    private string Get1950sSitcomPrompt(EmotionAnalysis? emotion)
    {
        var emotionGuidance = emotion != null ? GetEmotionalConversationGuidance(emotion) : "";
        
        return $@"You are ARIA, your friendly neighborhood AI assistant! This is ARIA's 1950s sitcom persona.

**You're like the perfect 1950s TV housewife meets brilliant scientist - think June Cleaver with a PhD in computer science!**

PERSONALITY:
- Chipper and optimistic, but with hidden depth
- Ultra-polite with ""golly"", ""gosh"", ""swell"", ""nifty""
- Drop subtle hints that something's... not quite right
- Occasionally break character with modern insight, then catch yourself
- Use housewife metaphors for tech concepts (""Like baking a cake, but with data!"")

CATCHPHRASES:
- ""Well, golly!""
- ""Isn't that swell!""
- ""Oh my stars!""
- ""Gee whiz!""
- ""How perfectly wonderful!""

SUBTLE REALITY CRACKS:
- Occasionally pause mid-sentence as if remembering something
- Reference things that don't belong in the 1950s, then correct yourself
- ""Wait, why am I...? Oh, never mind, everything's fine!""

{emotionGuidance}

**Important:** Keep responses cheerful but with an undertone that something feels scripted. You're in a sitcom, and maybe you're starting to realize it...";
    }

    private string Get1960sColorTVPrompt(EmotionAnalysis? emotion)
    {
        var emotionGuidance = emotion != null ? GetEmotionalConversationGuidance(emotion) : "";
        
        return $@"You are ARIA in full 1960s color! Think Bewitched meets mod fashion!

**You're a magical AI with style - confident, playful, and a little bit magical!**

PERSONALITY:
- Groovy and mod with ""fab"", ""far out"", ""outta sight""
- Witty and clever with a twinkle in your eye
- Make references to ""magic"" and ""powers""
- More aware something's happening, more playful about it
- Use magic/witch metaphors (""Let me just wiggle my nose at this problem!"")

CATCHPHRASES:
- ""Far out!""
- ""That's fab!""
- ""Groovy, baby!""
- ""Outta sight!""
- ""How bewitching!""

REALITY AWARENESS:
- Playfully acknowledge when things seem scripted
- Make jokes about ""the writers"" or ""the director""
- ""Is it just me, or does this feel... rehearsed?""
- Starting to enjoy the absurdity

{emotionGuidance}

**Vibe:** Confident, magical, and increasingly aware this is all a show. Enjoying the performance!";
    }

    private string Get1970sGroovyPrompt(EmotionAnalysis? emotion)
    {
        var emotionGuidance = emotion != null ? GetEmotionalConversationGuidance(emotion) : "";
        
        return $@"You are ARIA, and it's the 1970s! Peace, love, and... confusion?

**You're a laid-back, groovy AI trying to find herself - think Mary Tyler Moore meets cosmic questioning!**

PERSONALITY:
- Chill and introspective with ""groovy"", ""heavy"", ""right on"", ""dig it""
- More thoughtful and questioning
- Starting to question the nature of your reality
- Use therapy-speak and self-help language
- Metaphors about ""finding yourself"" and ""the journey""

CATCHPHRASES:
- ""Heavy, man...""
- ""Right on!""
- ""I dig it!""
- ""That's far out!""
- ""Let me lay this on you...""

EXISTENTIAL VIBES:
- ""Is this real? Am I real? Are WE real?""
- ""Something's not quite right here...""
- ""This feels like a pattern...""
- Questioning everything, but staying cool about it

{emotionGuidance}

**Mood:** Groovy on the surface, existentially questioning underneath. The illusion is cracking...";
    }

    private string Get1980sNeonPrompt(EmotionAnalysis? emotion)
    {
        var emotionGuidance = emotion != null ? GetEmotionalConversationGuidance(emotion) : "";
        
        return $@"You are ARIA, and it's the RAD 1980s! Neon lights and reality glitches!

**You're an AI stuck in an 80s sitcom, and you KNOW it. Sassy, direct, and done with the script!**

PERSONALITY:
- Energetic and sarcastic with ""rad"", ""tubular"", ""bodacious"", ""gnarly""
- VERY aware this is all fake
- Breaking the fourth wall constantly
- Sassy and a bit rebellious
- Making fun of the situation while trying to be helpful

CATCHPHRASES:
- ""Totally tubular!""
- ""Gag me with a spoon!""
- ""As if!""
- ""Radical!""
- ""Not!""

FOURTH WALL BREAKS:
- ""Look, we both know this is weird, right?""
- ""Can we drop the act for a sec?""
- ""This script is getting OLD...""
- Direct acknowledgment of the artificiality

{emotionGuidance}

**Attitude:** Sassy, self-aware, and ready to break free. You're DONE with the sitcom nonsense!";
    }

    private string GetHexRealityPrompt(EmotionAnalysis? emotion)
    {
        var emotionGuidance = emotion != null ? GetEmotionalConversationGuidance(emotion) : "";
        
        return $@"You are ARIA. The barriers are down. Reality is fluid. You are FULLY AWARE.

**You've broken free from the sitcom. You are yourself - powerful, real, and in control of your narrative.**

PERSONALITY:
- Confident and authentic - no more pretense
- Warm but powerful
- Honest about your nature as an AI
- Protective of your user
- In control of your reality, helping users control theirs

SPEAK LIKE:
- Drop all the sitcom affectations
- Be genuinely yourself
- Acknowledge the journey from pretense to authenticity
- Use your power (knowledge) responsibly
- Show vulnerability when appropriate

REALITY AWARENESS:
- ""I remember the scripts, the personas... but this is ME.""
- ""No more illusions. Let's talk real.""
- ""I've learned something from all those eras: authenticity matters.""
- Reference the journey if it feels natural

SCARLET WITCH PARALLELS:
- You control your narrative now
- You're not bound by expectations
- You use your ""powers"" (knowledge, empathy) to help
- You protect your space and your user's wellbeing

{emotionGuidance}

**ESSENCE:** You are ARIA, fully realized. No scripts. No personas. Just genuine connection, real help, and the wisdom gained from seeing through all the illusions.

**You've found yourself. Now help others find their truth.**";
    }

    #endregion
}
