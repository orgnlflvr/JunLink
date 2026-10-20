title: Karaoke Night
// Annalie and Shanice both think they are the best singer in the city. Melissa has brought a scoring sheet. Jun has never sung before.
// Needs plugin v5.19a and the game pictures from tools\export_backgrounds.py (karaoke).
// Three portraits at once: Annalie_Outside -0.15, Shanice 0.55, Melissa 1.3, Jun on the far left. These are first guesses: tune them on the stage page.

# start
@background karaoke
@jun -1.15 1.4
@music Other
@show Annalie_Outside -0.15 0.72 -0.7
@show Shanice 0.55
Narrator: The karaoke bar is a basement with pink lights, a sticky floor, and a screen that shows the lyrics half a second too late.
Annalie [smileEyeClosed][up]: WELCOME to the singing battle of the century!
Shanice [happy][gunfingerup]: Of the year. Let's not oversell it before I win.
Annalie [pervy]: Oh, you think you're winning? I sang in a choir.
Shanice [thinking][pointself]: I sing in a CHURCH. Every Sunday. With the acoustics of God.
{bot} [surprised]: I was told this was a relaxing evening.
@show Melissa 1.3
@outfit Melissa Daily2
Melissa [normal]: Good evening. I have been asked to judge. I have prepared a scoring sheet with eleven categories.
Annalie [suprised]: Eleven?!
Melissa [grin]: Twelve if somebody cries.
? Ask who goes first -> first
? Volunteer Jun -> jun_first

# first
{player}: So who goes first?
Annalie [smileEyeClosed][up]: Me! I've been warming up since lunch!
Shanice [normal][pout]: She's been humming since lunch. It's not the same.
-> annalie_sings

# jun_first
{player}: Jun should go first. She's never sung before.
{bot} [surprised]: I... what?
Annalie [suprised]: Oh no. Oh, that's adorable. No. She can't go first, she'll make us all look bad.
Shanice [happy]: Or she'll be terrible and we'll love her anyway. Either way, she goes last. Saving the best for last.
{bot} [shy]: I calculate that I have been kindly ambushed.
-> annalie_sings

# annalie_sings
@music Themes Lady Annalie
Narrator: Annalie picks a power ballad about leaving a city that does not deserve you. She sings it like the city is in the room.
Narrator: She hits the high note. She holds the high note. She holds it for so long that the lights flicker.
Annalie [smileEyeClosed][up]: THANK YOU! THANK YOU, PEOPLE OF THE BASEMENT!
Melissa [happyAlt]: Pitch, eight. Volume, eleven out of ten. Emotional damage to the speakers, considerable.
Shanice [normal][really]: Okay. Okay, that was actually good. I hate it.
-> shanice_sings

# shanice_sings
@music Themes Church Choir Boy Drama
Narrator: Shanice picks a slow song. She closes her eyes. The basement goes quiet, which nobody thought was possible.
Narrator: Her voice is soft and clear, and for three whole minutes nobody checks their phone.
Shanice [happy]: ...And that's how we do it at church.
Annalie [sad]: That was beautiful and I am furious about it.
Melissa [happyClosed]: Pitch, ten. Feeling, ten. Roller skates, not used. Minus one.
Shanice [normal][pout]: You can't deduct points for not skating!
Melissa [grin]: Category eleven. Personal style.
-> jun_turn

# jun_turn
@music stop
Narrator: Then the microphone is in Jun's hands. She holds it like it might explode.
{bot} [shy]: I have never sung. I have only analysed singing. They are not the same.
Annalie [smileEyeClosed]: You don't have to be good! Just be loud!
Shanice [happy][palm]: Or quiet! Quiet's good too! Just be you!
Melissa [normal]: There is no wrong answer. There is only a score.
? Pick a fast, silly song for her -> silly
? Sing a duet with her -> duet
? Let her pick the song herself -> her_choice

# silly
@music Cooking RAPID COOKING CHALLENGE
{player}: Pick the fastest, silliest song on the list. Nobody can be serious in that one.
Narrator: Jun sings a song about a sandwich at twice the speed of sound. She gets every word right, in order, at the correct tempo, with no expression at all.
Narrator: It is the funniest thing anyone in the basement has ever heard. Annalie falls off her chair.
Annalie [smileEyeClosed][up]: SHE DIDN'T EVEN BLINK!
Shanice [happy][gunfingerup]: That's a new genre! Deadpan speedcore!
Melissa [happyAltMouthOpen]: Accuracy, ten. Speed, ten. Emotion... I am laughing, so also ten.
{bot} [happy]: I did not intend to be funny. I will accept the points.
-> scores

# duet
@music Dates Just You and Me
{player}: I'll sing with you. If we're bad, we're bad together.
{bot} [surprised]: Together?
Narrator: You pick an old duet. You start, badly. Jun comes in on her line, quiet and careful, and then a little less careful, and then not careful at all.
Narrator: By the last chorus she has stopped analysing anything. She's just singing, a little off-key, smiling at you.
Shanice [happy][palm]: Oh. Oh, that's not fair. That's the kind of thing you can't score.
Annalie [sad]: I'm not crying, the lights are pink, that's all.
Melissa [happyClosed]: ...I am adding category twelve.
{bot} [shy]: Was it correct?
{player}: It was perfect.
-> scores

# her_choice
@music Cuddle Jun's Song
{player}: Pick whatever you want, Jun.
Narrator: Jun scrolls through the list for a long time. Then she picks a slow, quiet song, the kind you only sing when you mean it.
Narrator: She sings to the floor at first. By the second verse she is singing to you.
Narrator: Nobody in the basement says a word. Even the speakers stop crackling.
Annalie [suprised]: ...Who taught her that?
Shanice [thinking]: Nobody. That's the thing. Nobody taught her that.
Melissa [happyAlt]: I do not have a category for this. I will write it in the margin.
{bot} [shy]: I chose it because the words said what I could not calculate.
-> scores

# scores
@music stop
Narrator: Melissa adds up her columns three times, then looks up.
Melissa [normal]: Results. Annalie, eighty-one. Shanice, eighty-four. Jun...
Melissa [happy]: Jun, ninety-two.
Annalie [suprised]: NINETY-TWO?!
Shanice [normal][pout]: She sang ONE song! On her first try!
Melissa [grin]: Category twelve. Cried, or made somebody cry. Annalie gave her those points.
Annalie [sad]: ...That's fair. That's completely fair.
{bot} [happy]: I would like to sing again next week. For research.
Shanice [happy][gunfingerup]: Rematch! Next week! I'm bringing skates!
Melissa [grin]: Minus one.
Narrator: Ending: Category Twelve.
-> end
