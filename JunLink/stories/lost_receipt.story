title: The Lost Receipt
// Needs @show Annalie (her clothing-shop portrait). Her faces: normal, sad, surprised, pervy, happy. Her arm: [point] = up, [down] = down.
// Melissa has no on-screen portrait, so she talks through Annalie's phone.

# start
Narrator: Somebody knocks on the door. Three times, perfectly spaced.
{bot} [surprised]: Visitor detected. Probability of the landlord: sixty percent. Probability of a fashion emergency: forty.
@show Annalie left
Annalie [happy][point]: Surprise! Lady Annalie has arrived, and she brought drama!
{player}: Annalie? How did you even get in the building?
Annalie [pervy]: Charm.
Annalie [normal][down]: And the door was open. Mostly the door.
? Ask what the drama is -> drama
? Tell her it is a bad time -> badtime

# badtime
{player}: Annalie, it is kind of a bad time. Rent is due in two days...
Annalie [surprised]: Rent?!
Annalie [sad]: Oh no. Rent. The great villain of every city.
Annalie [normal]: This city eats people like us for breakfast, you know. One day I am going to leave and never look back.
Annalie [happy][point]: But not today! Today my drama pays in snacks.
-> drama

# drama
Annalie [normal]: Okay. So. Melissa called me from the supermarket.
Annalie [surprised][point]: She lost a receipt!
{bot} [point]: A receipt. That is a piece of paper with numbers on it.
Annalie [pervy][down]: Not just any receipt. It has the exact price of every item she ever scanned.
Annalie [sad]: She says it is "important for the records." Poor Melissa.
{player}: That sounds like the most Melissa thing ever.
Annalie [happy][point]: I put her on speaker!
Melissa: Good afternoon. This is Melissa. I am not allowed to talk on company time, but this is a technical emergency.
Melissa: The receipt is forty-one centimeters long. Last seen between aisle seven, shelf three and aisle seven, shelf four.
{bot} [lecture]: According to my calculations, a receipt that long does not get lost. It hides.
Melissa: I agree with that assessment. I did not want to say it first.
? Search the apartment -> apartment
? Ask Melissa to check her pockets -> pockets
? Blame the landlord -> landlord

# apartment
{player}: We can look here first. Maybe it followed Annalie in.
Annalie [surprised]: Followed me?!
Annalie [normal]: Wait...
Annalie [surprised][point]: Is that a receipt on my shoe?
Narrator: It is. It is forty-one centimeters long and a little bit sticky.
Melissa: That is my receipt. Please do not read it. It contains my private opinions about discount coupons.
{bot} [happy]: Mission complete. I request one point of friendship for finding it.
Annalie [happy][down]: Granted! And one extra point for the shoe!
-> found

# pockets
{player}: Have you checked your pockets, Melissa?
Melissa: One moment.
Narrator: A long pause. Paper rustles over the speaker.
Melissa: ...It was in my left pocket. Forty-one centimeters, folded eleven times.
Annalie [surprised]: Eleven folds?!
Annalie [happy][point]: That is basically origami!
Melissa: I will not discuss this further. Thank you for your help.
-> found

# landlord
{player}: It was probably the landlord. He charges three hundred dollars for a spare key. He would charge for a receipt too.
Annalie [pervy]: Ooh. A conspiracy.
Annalie [normal][point]: Let us interrogate him! I will bring a clipboard so I look official.
{bot} [surprised]: Probability that the landlord has the receipt: two percent. Probability that he will invoice us for the question: ninety-nine.
Annalie [sad]: ...Okay, no interrogation.
Melissa: For the record, the landlord does not shop at my register. He sends a different tenant to buy for him.
{player}: ...That is somehow worse.
-> pockets

# found
Melissa: The receipt has been recovered. I will now return to my duties. Goodbye.
Narrator: The line goes quiet.
Annalie [happy][point]: Best afternoon ever!
Annalie [normal]: Hey, Jun. Do you want to hear a song I wrote about receipts?
{bot} [happy]: I would like that. I will prepare my sensors. And a chair.
Annalie [pervy][down]: It has a very dramatic key change.
{player}: Please do not tell the landlord about the concert.
Narrator: Somewhere downstairs, a very loud voice sneezes.
@hide Annalie
Narrator: The end.
-> end
