title: Trick or Treat
// Halloween night. Annalie (indoor look) comes by in her Halloween outfit.
// Her faces: normal, happy (eyes closed), sad, surprised, pervy (sly grin). The landlord is a voice through the door, so no portrait needed.
// Colours come from the game; Annalie purple, Landlord brown, You blue.

# start
Narrator: Halloween night. Someone knocks. Then someone else knocks on top of it, much louder.
{bot} [surprised]: Two visitors detected. One is knocking in rhythm. The other is knocking in anger.
Landlord: OPEN UP! I know there is candy in there!
{player}: Great. It is the landlord and he is on a candy mission.
@show Annalie left
@outfit Annalie Halloween_Outfit
Annalie [happy][point]: Trick or treat!
Annalie [pervy]: I came as a witch. Do not let the hat fool you, I am only a little bit evil.
? Hand out candy -> candy
? Ask about the landlord -> landlord

# candy
{player}: I only have one bag. And it is... mostly expired.
Annalie [surprised]: Expired?!
Annalie [normal]: How expired?
{bot} [lecture]: The date on the bag says March of last year. According to my calculations, it is still sugar. Sugar does not know about calendars.
Annalie [happy]: That is the most reassuring thing a robot has ever said to me.
Annalie [pervy]: Give me three. For the road. And the hat.
-> door

# landlord
{player}: Why is the landlord yelling at my door?
Annalie [sad]: Oh. That. I may have told him there was free candy here.
Annalie [surprised]: I did not think he would actually come!
{bot} [point]: Your plan had a flaw. The landlord is motivated by money and candy has a cash value.
Annalie [normal]: ...Fair. I will make it up to you.
-> door

# door
Landlord: I am counting to three! One! Two!
{player}: He never says three. It is how he scares people.
Annalie [pervy][point]: Let me handle him. I have a spooky plan.
Annalie [happy]: Jun, do the robot voice. Say "the rent is cursed."
{bot} [point]: The rent is cursed.
Narrator: Silence on the other side of the door.
Landlord: ...What did she say?
Annalie [surprised]: It is working!
? Keep going -> curse
? Open the door and offer him one candy -> deal

# curse
{bot} [lecture]: The rent is cursed. All who collect it will be haunted by receipts.
Annalie [pervy]: Ooooh. Forty-one centimeters of them.
Landlord: ...I will come back tomorrow. In daylight.
Narrator: Heavy footsteps fade down the stairs.
Annalie [happy][point]: We won!
-> end_scene

# deal
{player}: Here. One candy. Please go away.
Landlord: One? It is a start. Now I will charge you three hundred for the wrapper.
Annalie [surprised]: He is invoicing the wrapper!
Annalie [normal]: I take it back. The rent is cursed. And so is he.
{bot} [surprised]: Probability that he leaves now: eighty percent. Probability that he leaves with the entire bag: also eighty. These add up because he is greedy.
Narrator: A long grumble. The footsteps go away, and so does the bag.
-> end_scene

# end_scene
Annalie [sad]: Oh no. He took the candy.
Annalie [happy]: But the hat stays. This was the best Halloween I have had since I moved to this awful city.
{player}: Maybe next year we leave the door unlocked less.
Annalie [pervy][point]: Maybe. But where is the drama in that?
{bot} [happy]: I will mark this night as a good memory. Category: spooky, mostly sugar.
@hide Annalie
Narrator: The end.
-> end
