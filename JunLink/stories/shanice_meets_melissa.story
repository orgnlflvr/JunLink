title: The Discount Incident
// Shanice comes to the supermarket with a repair list and a plan to charm her way to a discount. Melissa does not do discounts.
// Needs plugin v5.17b. Places: the supermarket (shop), the street (outside), the night cafe (cafenight, saved by tools\export_backgrounds.py).
// Shanice: needs plugin v5.17a or newer, where @show Shanice means her Nun_NewOutfit portrait (use @show ShaniceOld for her other look).
// Shanice's faces (from her portrait): happy, normal, serious, thinking, angry, plus pout and really (eyes). Arms: palm, pointself, gunfingerup, down. Needs plugin v5.17b. Positions are a first guess: tune them on the stage page.

# start
@background shop
@jun -1.05 1.4
@show Melissa 1.25
Narrator: The supermarket on a slow afternoon. One flickering light, one very straight queue, and Melissa at the till, scanning a can of beans as if it owes her money.
{bot} [lecture]: Shopping list: rice, eggs, and one replacement fuse for the kettle. Estimated time in store, six minutes.
Melissa [normal]: Good afternoon. Please place your items on the belt in order of weight.
Narrator: The door slides open. Someone skates in. Not walks. Skates.
@show Shanice 0.1
Shanice [happy][palm]: Hiiii! Is this the place with the good fuses? Priestbot said it's the place with the good fuses.
Melissa [shocked]: ...Wheels are not permitted inside the store.
Shanice [happy][gunfingerup]: They're not wheels, they're a lifestyle.
{bot} [surprised]: That is Shanice. She repairs bots at the church. She once fixed my left hand with a bobby pin and a song.
Shanice [happy]: Jun! My favourite customer! How's the hand? Still waving?
{bot} [wave]: Fully operational. I wave at least nine times a day.
? Say hello to Shanice -> hello
? Stay quiet and watch this unfold -> watch

# hello
{player}: Hey, Shanice. Shopping for the church?
Shanice [happy]: Repair supplies! Fuses, solder, one roll of tape, and a lollipop. The lollipop is for morale.
Shanice [thinking][pointself]: And I have a plan. I'm going to be so charming at the till that I get a discount.
{player}: At this till?
Shanice [normal][really]: Why? What's wrong with this till?
-> plan

# watch
Narrator: You say nothing. Somehow you already know this will be worth watching.
Shanice [happy][pointself]: Don't tell anyone, but I'm going to charm a discount out of the cashier. I've done it at every shop on this street.
{bot} [lecture]: Your success rate is high. However, this cashier has a success rate of zero discounts in four years.
Shanice [normal][really]: Zero? Like, a round zero?
-> plan

# plan
Narrator: Shanice glides to the till and drops her basket on the belt. The lollipop lands on top, like a flag on a mountain.
Shanice [happy][palm]: Hi! I love your hat. And your glasses. And your whole precise vibe. Very official.
Melissa [normal]: Thank you. Fuses, three. Solder, one. Tape, one. Lollipop, one. That will be forty-two credits.
Shanice [happy]: Forty-two for someone with such a lovely hat?
Melissa [hmmm]: The price does not change based on the hat.
Shanice [normal][pout]: What if I sing?
Melissa [normal]: The price does not change based on singing.
Shanice [normal][pout]: You haven't even heard me sing yet!
Melissa [grin]: I do not need to. I have a register.
{bot} [surprised]: This is the most balanced fight I have ever recorded.
? Help Shanice -> help_shanice
? Back up Melissa -> help_melissa
? Suggest a fair deal -> fair

# help_shanice
{player}: She fixes bots for the church almost for free. Doesn't that deserve something?
Melissa [hmmm]: Charity is not a discount category.
Shanice [happy][gunfingerup]: But it could be!
Melissa [normal]: ...I will consider adding it to the suggestion box. The suggestion box is opened once a year.
Shanice [thinking]: That's still a yes, technically.
-> scanner

# help_melissa
{player}: Sorry, Shanice. Melissa's been doing this for years. Prices are prices.
Melissa [happyAlt]: Thank you. That was a correct statement.
Shanice [serious]: Betrayed. In a supermarket. By a man holding eggs.
{bot} [lecture]: Statistically, eggs are the most common item to be holding during a betrayal.
-> scanner

# fair
{player}: What if she pays full price, and gives something back instead?
Melissa [hmmm]: Explain.
{player}: She's a repair tech. Anything in here broken?
Melissa [shocked]: ...
Narrator: Melissa glances down at her scanner. It is held together with two strips of tape and a lot of hope.
-> scanner

# scanner
Narrator: Melissa scans the lollipop. The scanner beeps, sputters, beeps again, and then gives up completely. A small curl of smoke rises from it.
Melissa [shocked]: No. No, no. It has worked for four years. It has a name.
Shanice [normal][really]: What's its name?
Melissa [depressed]: ...Kevin.
Shanice [happy][pointself]: Oh, I can save Kevin. Give me two minutes and that tape.
Melissa [hmmm]: You are not permitted behind the till.
Shanice [happy]: I'm not behind it. I'm leaning very far over it.
Narrator: Shanice pulls a tiny screwdriver from her hair, hums something off-key, and opens Kevin like a book. Jun leans in to watch.
{bot} [lecture]: Her technique is unorthodox. Her success rate is ninety-one percent. The other nine percent end with sparks.
Narrator: There are sparks. Then the scanner gives a clean, happy beep.
Melissa [happy]: Kevin.
Shanice [happy][gunfingerup]: Kevin lives!
Melissa [happyAlt]: ...Thank you. That was very precise work. Precise in a messy way.
Shanice [thinking]: So, about that discount...
Melissa [grin]: No.
Melissa [happyClosed]: But the lollipop has been registered as "damaged in repair." It is free. That is not a discount. That is an inventory correction.
Shanice [happy]: That is the most beautiful thing anyone has ever said to me in a supermarket.
? Head out with everyone when Melissa's shift ends -> after_shift
? Leave them to it and go home with Jun -> home

# after_shift
@hide all
@background outside
@jun -0.8 1.4
@music Simulated Happiness
Narrator: Melissa's shift ends at exactly six. She comes out at six oh one, in her own clothes, with a notebook under her arm.
@show Melissa 1.25
@outfit Melissa Daily2
@show Shanice 0.1
Shanice [happy][palm]: You came out! I thought you lived in there.
Melissa [normal]: I live two streets away. I counted the steps once. Eight hundred and twelve.
Shanice [normal][really]: Of course you did.
Shanice [thinking]: Hey, Annalie says you two are friends. She never told me you were funny.
Melissa [shocked]: I am not funny. I am accurate.
Shanice [happy]: Same thing, if you do it with a straight face.
{bot} [happy]: I predict that these two will become friends. Confidence: seventy-eight percent.
Melissa [hmmm]: Seventy-eight is low.
Shanice [happy][gunfingerup]: We'll get it to a hundred! Cafe! Now! I'm paying! With my free lollipop money!
-> cafe

# cafe
@hide all
@background cafenight
@jun -0.8 1.4
@music She's Real to Me
@show Melissa 1.25
@outfit Melissa Daily2
@show Shanice 0.1
Narrator: Four mismatched chairs, one table, three drinks and one lollipop. Shanice tells a long story about Priestbot getting stuck in a church bell. Melissa asks for exact measurements of the bell.
Melissa [happyAlt]: ...So the bell was ninety centimetres. And he is one metre twenty.
Shanice [happy]: That's why it was funny!
Melissa [grin]: I understand now. It is funny because of the numbers.
{bot} [lecture]: Updated prediction: ninety-four percent.
Shanice [thinking]: Melissa, what's your deepest secret?
Melissa [normal]: I once gave a customer a discount by accident. Eleven credits. I still have the receipt.
Shanice [normal][really]: You KEEP it?
Melissa [happyClosed]: I keep everything. It is how I remember that things happened.
Narrator: Shanice is quiet for a moment, which is new for everyone at the table.
Shanice [happy]: Then I'm getting you a receipt for today. So you remember this one too.
Melissa [happy]: ...I would like that.
Narrator: Ending: Kevin Lives.
-> end

# home
@hide all
@background room
@music Daybreak With You
Narrator: You leave them arguing about whether a lollipop can be "damaged in repair" when it was never in the repair.
{bot} [happy]: I would like to report something. I think Lady Shanice and Lady Melissa will be friends.
{player}: Shanice and Melissa? They are complete opposites.
{bot} [lecture]: Exactly. One breaks rules and fixes things. One keeps rules and keeps things. They cancel each other out into a stable system.
{bot} [shy]: A bit like us. I keep schedules. You lose them.
{player}: Hey.
{bot} [happy]: It was a compliment. Probably.
Narrator: Ending: A Stable System.
-> end
