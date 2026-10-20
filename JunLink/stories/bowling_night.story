title: Strike Night
// Needs plugin v5.14zc or newer (own background pictures) and the same commands as annalies_day.story.
// Put bowling.png from the jun_backgrounds folder of the zip into UserData\MdrgPanel\jun_backgrounds\ (BepInEx: BepInEx\jun_backgrounds\). Then "@background bowling" shows it. Without the file the story reports an error when you load it.
// Annalie_Outside at 0.1, Melissa at 1.25 in her hangout outfit (@outfit Melissa Daily; try Daily2 for another look), Jun on the left.
// Everyone is good at bowling. Choices only change HOW they win, not whether they can bowl.
// Music: outside = Mile High, tense frame = Just You Wait, ending = Just You and Me. Change or delete the @music lines if you like.

# start
@background outside
@jun -0.8 1.4
@music Mile High
Narrator: Somewhere above you a neon sign buzzes: "STRIKE ZONE". The E flickers. Tonight you are bowling against the landlord, who owns a very big ball and a very small heart.
{bot} [surprised]: Message log from Lady Annalie: "TEAM NIGHT. LANE SEVEN. I HAVE BOOKED US AGAINST THE LANDLORD'S TEAM." Again, all capitals.
{player}: Against the landlord?
{bot} [lecture]: His team has won this league three years in a row. Annalie has a grudge. I have a ball-trajectory model.
@show Annalie_Outside 0.1 0.72 -0.7
Annalie [happy][point]: You came! Tonight we take back the trophy and my honor!
Annalie [pervy]: Also, if we win, the landlord has promised to forget one month of rent. In writing. I made him sign a napkin.
{player}: A napkin is not a contract.
Annalie [normal]: A napkin with a signature and a coffee stain is the strongest contract in this city.
? Ask who the fourth player is -> fourth
? Ask how good everyone is -> skill

# fourth
{player}: A team needs four. Who is the fourth?
@show Melissa 1.25
@outfit Melissa Daily
Melissa [happyAlt]: I am the fourth. I was asked to join at nine this morning, and I arrived at eight fifty-two.
Annalie [happy]: I asked her because she counts everything. Every pin, every second, every excuse.
Melissa [grin]: Eleven pins in a clean spare is not an excuse. It is an outcome.
-> skill

# skill
@background bowling
@jun -0.8 1.4
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily
Annalie [pervy][point]: Warm-up rules! Everyone throws one ball so we can see what we have.
{bot} [lecture]: I will measure speed, angle and rotation. Please try not to bowl like a person who has never bowled.
{player}: I have bowled a lot, actually.
{bot} [surprised]: That information was not in my profile.
? Throw first -> you_throw
? Let Annalie go first -> annalie_throw

# you_throw
Narrator: You pick a ball that fits your fingers and breathe out. The approach is quiet. The ball rolls with a low hum, bends a little at the end, and the headpin falls like it was waiting.
Narrator: Strike. The pins all go down in one tidy crash.
{bot} [surprised]: Entry angle, six degrees. Pocket hit. This is statistically beautiful.
{player}: Practice, and a lot of quiet evenings.
Melissa [happyAlt]: Noted. You will bowl third.
-> annalie_throw

# annalie_throw
Annalie [happy][point]: Watch the drama!
Narrator: She spins the ball up to her hip, takes four steps like a dancer, and lets go with a flick of her wrist. The ball curves wide across the lane, almost into the gutter, then swings back in at the last second.
Narrator: Strike. The pins explode across the lane.
Annalie [pervy]: A hook shot. It looks like a mistake until it is not.
{bot} [surprised]: Your hook rate is eighty-four percent. That is above the expected level for someone with that many bracelets.
Annalie [happy]: The bracelets are aerodynamic!
-> melissa_throw

# melissa_throw
Narrator: Melissa places her feet on the marked boards, looks at the arrows on the lane for three seconds, and rolls the ball once. It does not curve. It does not wobble. It simply arrives.
Narrator: Strike. The headpin does not even have time to look surprised.
Melissa [happyAlt]: Eighteen pounds. Release at the second arrow. The lane was oily on the left, so I corrected by half a board.
Annalie [surprised]: You can see oil?
Melissa [grin]: I can see everything. It is called paying attention.
-> jun_throw

# jun_throw
{bot} [point]: My turn. I calculated this throw while you were talking.
Narrator: Jun picks up the ball with careful hands. She lines her feet up, rolls her shoulder once and releases at a height that looks like it was measured with a ruler.
Narrator: The ball travels in a straight line, a little slower than the others. It touches the pocket at exactly the right angle. Ten pins disappear.
{bot} [happy]: Strike. Probability of success: ninety-six percent. I will log the remaining four percent as "dramatic tension."
Annalie [happy][point]: She said "dramatic tension"! Jun is learning!
Melissa [shocked]: Her release height did not move by even two millimeters. I would like to see that again.
{player}: We might actually beat him.
Melissa [grin]: We will beat him by eleven pins. I have already calculated the margin.
-> match

# match
@music Just You Wait
@hide Annalie
@hide Melissa
Narrator: League night begins. The landlord arrives with two friends and an enormous ball he calls Lucille. He looks at your team like a man who is about to collect rent.
Landlord: Ha! I see the robot has been taught to roll. We will see how well it handles pressure.
{bot} [lecture]: His average is one hundred ninety-one. Ours, with the warm-up included, is one hundred ninety-eight. He is not as scary as he thinks.
Narrator: The frames go by in a blur of clean white pins and loud cheering. The two teams trade strikes until the last frame of the last game.
Narrator: The score is tied at two hundred and four. Everything depends on one final throw by your team.
Narrator: Annalie squeezes your arm. Melissa puts her hand on the ball return as if she is about to defuse it. Jun stands very still.
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily
Melissa [hmmm]: Two hundred four to two hundred four. The probability of a tie-break is unacceptably high.
Annalie [sad]: I cannot watch. I am going to watch through my fingers.
Melissa [hmmm]: Your fingers are a poor filter. Watch normally.
? Take the final throw yourself -> player_finish
? Let Jun take the final throw -> jun_finish
? Let Melissa take the final throw -> melissa_finish

# player_finish
{player}: I will do it.
Narrator: You step up. The alley goes quiet in the way that only a bowling alley can, with just the hum of the machines and a very faint pop song.
Narrator: You think about nothing, roll the ball, and watch it curve in a smooth arc towards the pocket.
Narrator: All ten pins fall. A perfect strike. The scoreboard flashes and the crowd roars.
{bot} [surprised]: Final score, two hundred fourteen to two hundred four. Your throw has been saved in my memory, category: heroic.
Annalie [happy][point]: THE NAPKIN IS ACTIVE!
Landlord: ...That is not a legal document.
Annalie [pervy]: Yes it is. It has a stain.
-> after

# jun_finish
{player}: Jun. It is your throw.
{bot} [surprised]: Me? The probability of success is ninety-six percent. I have always wanted to say the other four.
{player}: Just throw it.
Narrator: Jun takes a slow breath that she does not need. She rolls. The ball travels along the lane like a line on a graph and strikes the pocket without a sound.
Narrator: Ten pins fall in a perfect rolling collapse.
{bot} [happy]: Calculation complete. We win by ten pins. I experienced something that I will describe as "joy."
Annalie [happy][point]: SHE FELT JOY AT A BOWLING ALLEY! I am putting this in a poem!
Melissa [happyAlt]: The poem will have to include the score. Two hundred fourteen.
-> after

# melissa_finish
{player}: Melissa. This one is yours.
Melissa [normal]: I accept.
Narrator: She steps up without a word. Her form is exactly the same as in the warm-up. Not one board off. The ball rolls, kisses the headpin and sends the whole rack flying.
Narrator: A clean strike. The scoreboard lights up like a slot machine.
Melissa [depressed]: Predicted margin, eleven. Actual margin, ten. I am disappointed, but I will accept the win.
Annalie [happy][point]: SHE IS DISAPPOINTED ABOUT A WIN! I love her!
Melissa [tongue]: ...I would like that last sentence not to leave this lane.
{bot} [lecture]: Melissa's average is now above that of the entire league. I will update the model.
-> after

# after
@hide Annalie
@hide Melissa
@music Just You and Me
Narrator: The landlord stares at the scoreboard for a long time. Then he stares at the napkin, which Annalie is holding up like a flag.
Landlord: ...One month. No rent. But I want a rematch next season.
Annalie [happy][point]: DEAL!
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily
Annalie [pervy]: We should celebrate. Dinner is on me, and since I am broke, dinner is on whoever pays first.
Melissa [happyAlt]: I will pay for my own food. I will also pay for the shared fries, because the fries are logical.
{bot} [happy]: I would like to record one more thing. Tonight was the first time I stood in a group of people who were all good at something and none of them was showing off. It felt... warm.
{player}: Yeah. It was a good night.
? Ask if they want to do this every week -> weekly
? Walk home with Jun -> home

# weekly
{player}: Weekly league night? Our team against the world?
Annalie [happy][point]: A weekly league! We will need a name!
Melissa [happy]: "Precise Strikes." It is on the registration form already.
Annalie [surprised]: You filled in the form before I asked?
Melissa [happyClosed]: I filled it in when you booked the lane. I know you.
{bot} [lecture]: Team name accepted. I will draw the logo. It will be a bowling pin with very good posture.
-> finale

# home
{player}: I think I will head home. Jun, are you coming?
{bot} [happy]: Of course. I would like to walk. My legs are still computing the throw.
Annalie [happy]: Go! Go and be adorable somewhere else!
Melissa [happyAlt]: Goodnight. Please wash your hands after the shoes.
Narrator: You leave the neon behind, and Jun walks beside you, holding the lane receipt like a trophy.
-> finale

# finale
@hide Annalie
@hide Melissa
Narrator: Somewhere behind you, a strike rattles down a lane, and someone cheers for it.
Narrator: The end.
-> end
