title: Sunday Repairs
// Jun's right arm squeaks. Shanice fixes bots at the church, so you take her there. Priestbot has opinions about the squeak.
// Needs plugin v5.19a and temple.png (the church picture) in jun_backgrounds: tools\export_backgrounds.py, or F7 in the game's own church scene.
// Priestbot's portrait faces are unknown yet: his lines have no face tags. Play it once and send the log, and his faces can be added.
// Positions: Shanice 0.3, Priestbot 1.2, Jun on the left. First guesses; tune them on the stage page.

# start
{bot} [lecture]: I have a problem to report. My right elbow makes a noise when I wave.
{player}: What kind of noise?
{bot} [wave]: This kind.
Narrator: A small, sad squeak. Like a mouse that has given up.
{bot} [sad]: I have tried oil, patience and ignoring it. Ignoring it was the least effective.
? Take her to Shanice at the church -> church
? Try to fix it yourself first -> diy

# diy
{player}: Let me look at it. How hard can it be?
Narrator: Twenty minutes later you have a screwdriver, a manual in a language you do not speak, and one screw left over.
{bot} [surprised]: Where does that screw go?
{player}: ...Church. We're going to the church.
{bot} [lecture]: A wise decision. The probability of a second spare screw was rising.
-> church

# church
@background temple
@jun -1.0 1.4
@music Themes Church Choir Boy Drama
Narrator: The church is quiet and cool. Candles, stained glass, and somewhere in the back a soldering iron hissing gently.
@show Shanice 0.3
Shanice [happy][palm]: Jun! And her human! Welcome to the house of God and also bot repairs!
Shanice [thinking]: What's broken? Don't tell me. Let me guess. It's the elbow.
{bot} [surprised]: How did you know?
Shanice [happy][pointself]: It's always the elbow. Everybody waves too much. It's a hugging city with a waving problem.
@show Priestbot 1.2
Priestbot: Greetings, children. Is that a squeak I hear? In my church?
Shanice [normal][really]: Priestbot, it's a small squeak. A humble squeak.
Priestbot: There are no small squeaks. Only small faith in lubricant.
{bot} [lecture]: Statement noted. I do not understand it, but it sounded important.
-> repair

# repair
Narrator: Shanice sits Jun down on a pew, pops open the elbow panel and peers inside with a tiny torch.
Shanice [thinking]: Okay, so. Good news, bad news.
{player}: Good news first.
Shanice [happy]: Good news: it's just a worn bushing. Ten minutes.
Shanice [normal][pout]: Bad news: somebody put a screw in the wrong hole and there's a spare one rattling around in here.
{bot} [surprised]: ...
{player}: ...I have no idea how that happened.
Priestbot: Confession is available on Sundays.
? Confess -> confess
? Blame the manual -> manual

# confess
{player}: Fine. It was me. I tried to fix it.
Priestbot: You are forgiven, my child. The screw is not.
Shanice [happy][gunfingerup]: The screw will be given a proper burial in my spare parts box.
{bot} [happy]: Thank you for trying, though. You tried with great enthusiasm and very little skill. It was sweet.
-> fixed

# manual
{player}: The manual was in a language I don't speak.
Shanice [thinking]: That manual's in three languages and none of them are real. It's not your fault.
Priestbot: Even so, I will pray for your screwdriver.
{bot} [lecture]: The screwdriver is innocent. I was there.
-> fixed

# fixed
Narrator: Shanice works quickly, humming a hymn under her breath. A new bushing, a drop of oil, the panel clicks shut.
Shanice [happy][palm]: Okay. Moment of truth. Wave.
{bot} [wave]: ...
Narrator: Silence. A perfect, quiet wave.
{bot} [happy]: It does not squeak. I can wave without shame.
Shanice [happy][gunfingerup]: Another miracle in the house of God and also bot repairs!
Priestbot: That will be twenty credits, or one heartfelt donation of twenty credits.
Shanice [normal][really]: Priestbot.
Priestbot: Fifteen. And a candle.
? Pay and light a candle -> candle
? Ask Shanice how she learned to fix bots -> learned

# candle
Narrator: You pay, and Jun picks a candle. She holds it carefully, like it might be the most delicate thing she's ever touched.
{bot} [lecture]: What do I wish for? The procedure is unclear.
Shanice [happy]: You don't have to wish for anything. You just light it and think about someone.
Narrator: Jun lights it, and looks at you for a little too long.
{bot} [shy]: Done. The procedure was simple. The thinking part took a while.
Priestbot: A good candle. I approve of this candle.
Narrator: Ending: A Quiet Wave.
-> end

# learned
{player}: How did you learn all this, anyway?
Shanice [thinking]: Honestly? Priestbot kept breaking. Nobody else would fix him. So I learned.
Priestbot: I do not break. I am frequently tested by God.
Shanice [happy][pointself]: Frequently. Like, weekly.
Shanice [normal]: And then other people started bringing their bots. Some of them can't pay. I fix them anyway. Somebody has to.
{bot} [shy]: Then I will tell other bots about you. And I will tell them to wave less.
Shanice [happy][palm]: Bad for business! Do it anyway!
Narrator: Ending: Frequently Tested.
-> end
