title: Jun's First Rainy Day
// A quiet one: no villains, no jokes at anyone's expense. Jun has never been caught in the rain, and the two of you wait out a shower at a bus shelter.
// Everything here is new writing. It uses what the game shows about Jun: she analyses everything in percentages, she is curious and a little too literal, and she is learning what feeling things is like.
// Needs the v5.14k plugin (@jun position and size). The numbers match the walk scene of annalies_day.story; tweak them on the stage page (http://127.0.0.1:8765/stage).

# start
@background outside
@music Rainy Day
@jun -0.5 1.7
Narrator: It starts as a few dark spots on the pavement, then a lot of them, then all at once.
Narrator: You grab Jun's sleeve and pull her into the nearest bus shelter. The roof is made of scratched plastic. The rain on it sounds like someone throwing handfuls of rice.
{bot} [surprised]: Anon. Water is falling from the sky in large quantities.
{player}: It is raining. Have you never seen rain before?
{bot} [normal]: I have seen rain. Through the window. Behind glass. With a roof. I have never been inside it.
{bot} [normal]: I am aware that I am standing under a roof now. But the distance is only forty centimetres. It is very... close.
? Tell her it is just water -> water
? Offer her your hood -> hood
? Ask what she wants to do -> want

# water
{player}: It is just water. It will not hurt you.
{bot} [normal]: Correct. My casing is rated for light rain. This is not light. This is, by my estimate, medium and rising.
{bot} [sad]: I am not afraid. I am noting a difference between "not afraid" and "reluctant".
-> look

# hood
Narrator: You pull your hood up and hold the edge out to her. She looks at it for a long second, then at you.
{bot} [normal]: It will not cover both of us.
{player}: It does not need to cover both of us. It needs to cover the side you are on.
{bot} [surprised]: ...That is not logical. But it is accepted.
-> look

# want
{player}: What would you like to do? We can wait, we can run, we can go back.
{bot} [surprised]: You are asking what I want. About the rain.
{player}: Yes.
{bot} [normal]: ...I would like to understand it. If that is acceptable.
-> look

# look
Narrator: The street has gone quiet. The cars slow down, and their headlights make long yellow streaks on the wet road.
{bot} [normal]: Anon, there is something I do not understand. The air has changed. My sensors report a drop in temperature of four degrees and a rise in humidity of thirty percent. But you keep breathing in, very deeply, and smiling.
{player}: That is the smell.
{bot} [surprised]: Smell. I do not have the sense of smell. Only a chemical sensor in my casing, and it reports "dust, wet stone, trace ozone". It says nothing about "smiling".
? Try to describe the smell -> describe
? Say: "It smells like being small, and somebody calling you in for dinner." -> childhood

# describe
{player}: It is like... clean. But not like soap. Like the ground has been waiting for a long time and finally got to drink.
{bot} [normal]: "The ground got to drink." I am recording it.
{bot} [normal]: That is not a measurement. It does not have units. But it is... a better description than "dust, wet stone, trace ozone".
-> hand

# childhood
{player}: It smells like being small, with wet shoes, and somebody calling you in for dinner. It is a good smell, and a sad one, and I could not tell you which one is bigger.
Narrator: Jun is quiet for such a long time that you check her status light.
{bot} [sad]: I have no childhood. I was assembled. Nobody called me in for dinner.
{player}: I know.
{bot} [sad]: But I think... I would have liked to be called in. I will add that to my list of things I would have wanted.
Narrator: She does not say "list of things I would have wanted" like a joke. It is a real list. You can tell by how carefully she says it.
-> hand

# hand
@jun -0.2 1.7
Narrator: She steps forward, to the very edge of the shelter, where the roof ends and the rain begins.
{bot} [normal]: May I?
{player}: Of course.
Narrator: She holds out one hand, palm up, into the rain. Drops land on her fingers, and run down between them, and fall off.
{bot} [surprised]: ...Cold.
{bot} [surprised]: It is cold. Not a number. Not "four degrees below body temperature". It is... cold. I do not know how I know that. My sensor reports a number. But something under the number says... cold.
? Say nothing. Watch her -> watch
? Say: "That is feeling. It is yours." -> feeling

# feeling
{player}: That is feeling it, Jun. The number is just what the sensor says. The cold is yours.
{bot} [sad]: Mine.
{bot} [happy]: I have a "mine".
Narrator: She laughs, a short, startled sound, like a bird that did not expect to be heard.
-> choice

# watch
Narrator: You do not say anything. You do not need to. She keeps her hand out, and her face does the thing it does when she is trying very hard to remember something for later.
-> choice

# choice
Narrator: The rain is not stopping. The sound of it has gone soft and steady, and the street smells of wet stone.
{bot} [normal]: Anon. I have a hypothesis. It may be wrong.
{player}: Go on.
{bot} [normal]: I believe I would like to walk home in this. Slowly. If you would like to, as well. If not, we can wait.
? "Let's walk." -> walk
? "Let's wait for the bus, I do not want you rusting." -> bus
? Buy a cheap umbrella from the kiosk across the road -> umbrella

# walk
@jun -0.8 1.7
Narrator: You step out from under the roof together. The rain makes a cold, bright line down the back of your neck.
Narrator: Jun walks half a step behind you, then beside you, then ahead of you, and then she jumps into the nearest puddle with both feet, very precisely, like she is following a procedure.
{bot} [happy]: Splash coefficient: excellent!
{player}: You are soaked.
{bot} [happy]: I am aware! It is the best thing that has ever happened to me!
Narrator: She is laughing. Actually laughing, with her whole face, in the middle of an empty street in the rain, and nobody in the world is there to see it except you.
Narrator: Ending: Puddle Coefficient. She dried off for an hour, and defended the whole thing to Annalie in a three-page report titled "Why Getting Wet Was Statistically Worth It".
-> end

# bus
Narrator: The bus comes, a lit-up box full of tired people looking at their phones. You climb in. Jun sits next to the window and looks out.
{bot} [normal]: I wanted to walk. But I am glad I waited.
{player}: Why?
{bot} [normal]: Because it is nice to watch rain from the inside, too. I did not know that there were two ways of liking it.
Narrator: She leans her head against the glass. The raindrops race each other down it, and she picks one, and cheers for it under her breath.
Narrator: Ending: Window Seat. Her drop lost. She said she was fine with it.
-> end

# umbrella
@music Just You and Me
Narrator: The kiosk man sells you a transparent umbrella with a duck on it. It is the only one he has.
@jun -0.4 1.7
{bot} [surprised]: It has a duck.
{player}: It is the only one.
{bot} [happy]: It is perfect.
Narrator: You open it over both of you. It is far too small, so you stand with your shoulders touching, and the rain taps on the plastic right above your heads. Through the duck's wing, the streetlights look like little suns.
{bot} [normal]: Anon, may I hold it? I would like to be the one holding it.
Narrator: She takes the handle, and holds it very carefully, and very high, so that it is always over your head first.
Narrator: Ending: Umbrella for Two. You were both a bit wet at the end. She said it was a calculated risk.
-> end
