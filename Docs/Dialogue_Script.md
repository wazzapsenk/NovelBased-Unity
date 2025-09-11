# College Chronicles - Prototype Dialogue Script

## Character Profiles

### Main Character (MC) - Alex Chen
- **Age**: 19, College Freshman
- **Background**: Smart, ambitious, but still finding their place in college
- **Voice**: Relatable, sometimes uncertain, but eager to grow
- **Personality Range**: Can develop toward Alpha (confident, leadership) or Beta (empathetic, collaborative)

### NPC_A - Riley Martinez
- **Role**: Friendly roommate/study buddy
- **Personality**: Optimistic, social, supportive
- **Expressions**: Neutral, Happy
- **Voice**: Encouraging, uses casual language, lots of "dude" and "totally"

### NPC_B - Jordan Kim
- **Role**: Competitive classmate/rival
- **Personality**: Intense, perfectionist, easily frustrated
- **Expressions**: Neutral, Angry
- **Voice**: Direct, formal, achievement-focused

---

## Scene 1: Dorm Room - Introduction (~5 minutes)

### Node 1.1 - Opening (MC Alone)
**Speaker**: Narrator  
**Text**: "First week of college at Riverside University. Your dorm room still smells like fresh paint and possibility. A stack of textbooks sits on your desk, and your phone buzzes with notifications from classmates you barely know."

**Actions**: None  
**Next**: Auto-continue to Node 1.2

### Node 1.2 - Morning Reflection (MC)
**Speaker**: MC  
**Expression**: Neutral  
**Text**: "Okay, Alex. This is it. Time to figure out who you're going to be in college. High school feels like a lifetime ago, but here... here I can be anyone I want."

**Actions**: None  
**Next**: Auto-continue to Node 1.3

### Node 1.3 - Riley Enters (Riley)
**Speaker**: Riley Martinez  
**Expression**: Happy  
**Text**: "Morning, roomie! Dude, you're up early. I was just heading to grab coffee before our Psychology 101 lecture. Want to come with? Professor Martinez is supposed to be intense."

**Choices**: 
- Choice A (Alpha): "Lead the way. I like intense professors - they push you to excel." 
  - Impact: +1 Alpha
  - Next: Node 1.4A
- Choice B (Beta): "That sounds great! I'm nervous about meeting everyone, so having you there would be awesome."
  - Impact: +1 Beta  
  - Next: Node 1.4B
- Choice C (Neutral): "Sure, coffee sounds good. I could use the caffeine."
  - Impact: None
  - Next: Node 1.4C

### Node 1.4A - Alpha Response (Riley)
**Speaker**: Riley Martinez  
**Expression**: Happy  
**Text**: "I love that attitude! You're going to do great here. Come on, let's go show this place what we're made of."

**Next**: Auto-continue to Node 1.5

### Node 1.4B - Beta Response (Riley)  
**Speaker**: Riley Martinez  
**Expression**: Happy  
**Text**: "Aw, don't worry! Everyone's super friendly here. I'll introduce you to some people - we're all figuring this out together."

**Next**: Auto-continue to Node 1.5

### Node 1.4C - Neutral Response (Riley)
**Speaker**: Riley Martinez  
**Expression**: Neutral  
**Text**: "Totally! The coffee at the student center is actually not terrible. Plus, I heard they have those fancy pastries today."

**Next**: Auto-continue to Node 1.5

### Node 1.5 - Walking to Coffee (MC)
**Speaker**: MC  
**Expression**: Neutral  
**Text**: "This campus is huge. I keep getting lost trying to find my classes. Yesterday I ended up in the wrong building entirely."

**Actions**: None  
**Next**: Auto-continue to Node 1.6

### Node 1.6 - Riley's Encouragement (Riley)
**Speaker**: Riley Martinez  
**Expression**: Happy  
**Text**: "Dude, everyone does that! I once spent twenty minutes looking for the library and it was literally behind me the whole time. You'll get the hang of it."

**Actions**: None  
**Next**: Auto-continue to Node 1.7

### Node 1.7 - Jordan Appears (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Alex Chen? I'm Jordan Kim from your Economics class. I wanted to discuss the group project Professor Davis assigned. I've already outlined a preliminary strategy."

**Choices**:
- Choice A (Alpha): "Great initiative, Jordan. I appreciate thorough planning. Let's hear your strategy."
  - Impact: +1 Alpha
  - Next: Node 1.8A
- Choice B (Beta): "Wow, you work fast! I'd love to hear everyone else's ideas too. What does the rest of our group think?"
  - Impact: +1 Beta
  - Next: Node 1.8B
- Choice C (Neutral): "Cool, what did you have in mind?"
  - Impact: None
  - Next: Node 1.8C

### Node 1.8A - Alpha Project Response (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Excellent. I've researched three potential topics and created a timeline. If we start immediately, we can secure the best resources before other groups."

**Next**: Auto-continue to Node 1.9

### Node 1.8B - Beta Project Response (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "I... hadn't considered consulting the others yet. I suppose collaborative input could strengthen the proposal. Though we shouldn't waste time with indecision."

**Next**: Auto-continue to Node 1.9

### Node 1.8C - Neutral Project Response (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "I've identified market analysis of sustainable energy as our optimal topic. High impact, strong data availability, clear competitive advantage."

**Next**: Auto-continue to Node 1.9

### Node 1.9 - Phone Notification Trigger (MC)
**Speaker**: MC  
**Expression**: Neutral  
**Text**: "This sounds really—" *BUZZ BUZZ* "Oh, sorry, my phone's going off."

**Actions**: Phone notification triggers  
**Next**: Auto-continue to Node 1.10

### Node 1.10 - Branching Point Setup (Riley)
**Speaker**: Riley Martinez  
**Expression**: Happy  
**Text**: "No worries! Jordan, want to grab coffee with us? We can talk more about the project over pastries."

**Actions**: Start free-roam mode in Dorm Room  
**Next**: Free roam begins

---

## Phone Message Content

### Message Chain - "Come to Campus Quad"
**From**: Unknown Number  
**Message 1**: "Hey Alex! It's Sam from orientation week."  
**Message 2**: "Come to the campus quad when you get a chance!"  
**Message 3**: "There's something happening you don't want to miss 😊"  

---

## Scene 2: Campus Quad - Minigame Integration

### Node 2.1 - Arrival at Quad (MC)
**Speaker**: MC  
**Expression**: Neutral  
**Text**: "The campus quad is buzzing with activity. Students are gathered around something... is that music?"

**Next**: Auto-continue to Node 2.2

### Node 2.2 - Jordan's Surprise (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Alex! I didn't expect to see you here. This spontaneous dance gathering is... highly irregular. Though I admit, the rhythm is mathematically interesting."

**Next**: Auto-continue to Node 2.3

### Node 2.3 - The Challenge (Narrator)
**Speaker**: Narrator  
**Text**: "A group of upperclassmen are showcasing their dance moves, challenging newcomers to match their rhythm. Jordan looks both fascinated and terrified."

**Choices**:
- Choice A (Alpha): "Come on Jordan, let's show them what we can do! Sometimes you have to take risks."
  - Impact: +1 Alpha
  - Next: Node 2.4A
- Choice B (Beta): "Want to try this together, Jordan? It might be fun, and I'll be right there with you."
  - Impact: +1 Beta
  - Next: Node 2.4B

### Node 2.4A - Alpha Pre-Minigame (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Your confidence is... inspiring. Very well. Let's demonstrate our capabilities."

**Actions**: Start Rhythm Dance Minigame  
**Next**: Minigame results

### Node 2.4B - Beta Pre-Minigame (Jordan)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Together? I... yes. Yes, that makes it less intimidating. Thank you for the support."

**Actions**: Start Rhythm Dance Minigame  
**Next**: Minigame results

---

## Post-Minigame Dialogue Paths

### Success Path (>60% accuracy)
**Speaker**: Jordan Kim  
**Expression**: Happy  
**Text**: "We did it! That was... actually exhilarating. I never thought I'd enjoy something so unstructured. Thank you for encouraging me to try."

**Next**: Prototype End Screen

### Failure Path (≤60% accuracy or Skip)
**Speaker**: Jordan Kim  
**Expression**: Neutral  
**Text**: "Well, that didn't go as planned, but... it was worth attempting. Sometimes the effort matters more than the outcome."

**Next**: Prototype End Screen

---

## Asset Requirements Summary

### Character Expressions Needed:
- MC: Neutral
- Riley Martinez: Neutral, Happy  
- Jordan Kim: Neutral, Happy, Angry

### Scene Backgrounds:
- Dorm Room (Day)
- Campus Quad (Day)

### UI Elements:
- Phone notification icon
- Messaging interface mockup
- Rhythm game arrow indicators

### Audio Cues:
- Phone buzz sound
- Background campus ambience
- Rhythm game music track
- UI click sounds

---

## Personality Impact Summary

### Alpha Choices (+1 each):
1. "Lead the way. I like intense professors"
2. "Great initiative, Jordan. I appreciate thorough planning"
3. "Come on Jordan, let's show them what we can do!"

### Beta Choices (+1 each):
1. "That sounds great! I'm nervous about meeting everyone"
2. "I'd love to hear everyone else's ideas too"
3. "Want to try this together, Jordan?"

### Total Possible Range:
- Maximum Alpha: +3 points
- Maximum Beta: +3 points
- Mixed approaches possible for balanced character development