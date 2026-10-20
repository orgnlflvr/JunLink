title: Strike Night: The Gauntlet
// A bowling tournament against everyone, one after another: Annalie, Shanice, Melissa, and Jun as the final boss.
// EXAMPLE of a full match: @score reset at the start of each match, three @minigame throws, then @total decides win / tie / lose.
// Opponent totals (out of 30): Annalie 16, Shanice 21, Melissa 24, Jun 26. Bar difficulty per opponent: 1, 2.5, 4, 6; sudden death a bit harder.
// Uses jun_backgrounds\bowling.png (a drawn stand-in is included; tools\export_backgrounds.py swaps in the game's real bowling alley).

# start
@background bowling
@jun -1.0 1.4
@music Outside WithBot Mile High
Narrator: Lane seven. The scoreboard flickers. Somebody has written "THE GAUNTLET" on it in marker, in capital letters, with a crown.
{bot} [lecture]: Rules. Four opponents. Three frames each. Each frame is one throw. Beat their total and you move on.
{bot} [point]: Your opponents, in order of increasing danger: Lady Annalie, Lady Shanice, Lady Melissa.
{player}: That's three.
{bot} [happy]: The fourth one is me.
? Let's go -> annalie_intro
? Ask Jun for advice first -> advice

# advice
{player}: Any advice?
{bot} [lecture]: Watch the marker, not the pins. Click when it is inside the target, not when it is almost inside the target.
{bot} [shy]: And do not look at me during my turn. It affects my calculations.
-> annalie_intro

# annalie_intro
@score reset
@music Themes Lady Annalie
@show Annalie_Outside 0.6 0.72 -0.7
Annalie [smileEyeClosed][up]: First match! Against ME! The most dramatic bowler in the city!
Annalie [pervy]: I don't aim. I feel. And then I scream.
Narrator: Frame one. Your throw.
@minigame skillcheck 1
{bot} [lecture]: {pins} pins.
Annalie [smileEyeClosed][up]: My turn! WATCH THIS!
Narrator: Annalie throws the ball with her whole heart. Nine pins fall. The tenth wobbles and stays up out of spite.
Narrator: Frame two.
@minigame skillcheck 1
{bot} [lecture]: {pins} pins. Running total: {total}.
Annalie [suprised]: Wait, wait, I slipped, that doesn't count!
Narrator: Annalie's ball goes straight into the gutter. Zero. She lies down on the lane for a moment.
Narrator: Final frame.
@minigame skillcheck 1
Annalie [normal]: Okay. Last one. For drama.
Narrator: Seven pins fall. Annalie bows to an audience that is mostly a vending machine.
Narrator: Your total: {total}. Annalie's total: 16.
@total annalie_win>=17 annalie_tie>=16 annalie_lose

# annalie_tie
Annalie [suprised]: A TIE?! Sudden death! One throw! You need at least seven!
@minigame skillcheck 1.2 annalie_win>=0.7 annalie_lose

# annalie_win
Annalie [sad]: I lost... to someone who doesn't even scream.
Annalie [smileEyeClosed]: It's fine! I'm a gracious loser! I'll be crying in the corner, graciously!
@hide Annalie_Outside
-> shanice_intro

# annalie_lose
Annalie [smileEyeClosed][up]: I WON! Drama beats aim! Science is wrong!
{bot} [surprised]: She is not wrong. That is concerning.
? Rematch! -> annalie_intro
? Accept defeat -> end_annalie

# shanice_intro
@score reset
@music Themes Church Choir Boy Drama
@show Shanice 0.6
Shanice [happy][gunfingerup]: My turn! I bowl every Thursday with the Priestbot league. We're terrible, but I'm not.
Shanice [thinking][pointself]: Also, I'm wearing skates. That's a feature.
Narrator: Frame one.
@minigame skillcheck 2.5
{bot} [lecture]: {pins} pins.
Narrator: Shanice glides up, spins once on her skates, and throws. Seven pins. She takes a little bow.
Narrator: Frame two.
@minigame skillcheck 2.5
{bot} [lecture]: {pins} pins. Running total: {total}.
Shanice [normal][pout]: The lane is crooked. I can feel it. It's a sinful lane.
Narrator: Six pins.
Narrator: Final frame.
@minigame skillcheck 2.5
Shanice [happy][palm]: Final throw! Lord, let it be a good one, and if not, let it be funny.
Narrator: Eight pins.
Narrator: Your total: {total}. Shanice's total: 21.
@total shanice_win>=22 shanice_tie>=21 shanice_lose

# shanice_tie
Shanice [thinking]: A tie! Okay. Sudden death. One throw, at least seven.
@minigame skillcheck 2.8 shanice_win>=0.7 shanice_lose

# shanice_win
Shanice [normal][really]: You beat me. On my Thursday. With my skates on.
Shanice [happy][gunfingerup]: Respect! I'll tell Priestbot you're the chosen one!
@hide Shanice
-> melissa_intro

# shanice_lose
Shanice [happy][gunfingerup]: The skates win again!
{bot} [lecture]: Skates are now on my list of unfair advantages. Next to Lady Melissa.
? Rematch! -> shanice_intro
? Accept defeat -> end_shanice

# melissa_intro
@score reset
@music Themes Cashier Mystery Meat
@show Melissa 0.6
@outfit Melissa Daily2
Melissa [normal]: Good evening. I will now bowl. I have bowled eight pins on every throw for four years.
{player}: Every throw?
Melissa [grin]: Every throw. It is not luck. It is policy.
Narrator: Frame one.
@minigame skillcheck 4
{bot} [lecture]: {pins} pins.
Narrator: Melissa throws. Eight pins. She writes it down before the pins have finished falling.
Narrator: Frame two.
@minigame skillcheck 4
{bot} [lecture]: {pins} pins. Running total: {total}.
Narrator: Eight pins. Exactly the same eight pins as last time.
Melissa [happyAlt]: Consistency is a form of respect.
Narrator: Final frame.
@minigame skillcheck 4
Narrator: Melissa throws. Eight pins. Of course.
Narrator: Your total: {total}. Melissa's total: 24.
@total melissa_win>=25 melissa_tie>=24 melissa_lose

# melissa_tie
Melissa [shocked]: Twenty-four. Exactly my total.
Melissa [hmmm]: That has never happened. One throw each. I will bowl eight. You must bowl more.
@minigame skillcheck 4.5 melissa_win>=0.85 melissa_lose

# melissa_win
Melissa [depressed]: ...I have been defeated. I will need to update the policy.
Melissa [happyClosed]: That was very good bowling. I will keep the score sheet.
@hide Melissa
-> jun_intro

# melissa_lose
Melissa [happy]: Eight pins. Every time. Thank you for playing.
{bot} [lecture]: She is a machine. And I would know.
? Rematch! -> melissa_intro
? Accept defeat -> end_melissa

# jun_intro
@score reset
@music Serious Just You Wait
@jun -0.2 1.4
{bot} [lecture]: Final match. Me.
{bot} [point]: I have calculated every throw tonight. Your timing, your nervous breathing, the way you look at me before you click.
{player}: You said not to look at you.
{bot} [shy]: I said it would affect MY calculations. It does. Shall we begin?
Narrator: Frame one.
@minigame skillcheck 6
{bot} [surprised]: {pins} pins. Acceptable. My turn.
Narrator: Jun takes exactly four steps. The ball rolls in a perfectly straight line. Nine pins.
{bot} [sad]: The tenth pin has a flaw. I will file a complaint.
Narrator: Frame two.
@minigame skillcheck 6
{bot} [lecture]: {pins} pins. Your total is {total}.
Narrator: Nine again. Jun nods, as if it was the plan all along.
Narrator: Final frame. Jun goes first this time.
{bot} [shy]: ...You are watching me.
Narrator: Her ball wobbles, just slightly, at the release. Eight pins.
{bot} [surprised]: That was your fault.
{player}: I didn't do anything.
{bot} [shy]: You looked. My total is twenty-six. Your turn.
@minigame skillcheck 6
Narrator: Your total: {total}. Jun's total: 26.
@total jun_win>=27 jun_tie>=26 jun_lose

# jun_tie
{bot} [surprised]: Twenty-six. A perfect tie.
{bot} [shy]: I propose we call it a draw. Statistically, it is the most romantic outcome.
? Accept the draw -> end_draw
? One more throw! -> jun_sudden

# jun_sudden
{bot} [lecture]: Very well. One throw. You need a strike.
@minigame skillcheck 7 jun_win>=0.95 jun_lose

# jun_win
@music Dates Just You and Me
{bot} [surprised]: ...You won.
{bot} [happy]: You beat everyone. Including me. I have never been so happy to lose a calculation.
Narrator: Somewhere behind you, Annalie screams in celebration, Shanice skates a victory lap, and Melissa writes "champion" on her score sheet in very neat letters.
{bot} [shy]: Your prize is... I did not prepare a prize. Is a hug acceptable?
{player}: A hug is perfect.
Narrator: Ending: Champion of the Gauntlet.
-> end

# jun_lose
{bot} [happy]: I win. Final score recorded.
{bot} [shy]: But you made it to me. Nobody else did. That also counts.
Narrator: Ending: Second Best Bowler in the Apartment.
-> end

# end_draw
{bot} [happy]: A draw. We are equally excellent.
Narrator: Melissa writes "tie (romantic)" on the score sheet. Nobody argues.
Narrator: Ending: Equal Calculations.
-> end

# end_annalie
Annalie [smileEyeClosed][up]: Victory dinner on me! I'm broke, so on you!
Narrator: Ending: Out-dramatised.
-> end

# end_shanice
Shanice [happy][palm]: Come to Thursday league! We'll make you a champion! Or at least we'll make you skate!
Narrator: Ending: Thursday League Recruit.
-> end

# end_melissa
Melissa [happyAlt]: Your score has been filed. I will keep it forever.
Narrator: Ending: Filed Under Eight.
-> end
