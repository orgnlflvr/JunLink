title: A Day at Annalie's
// Needs the BackgroundFade version of the plugin. Places: your room, outside, Annalie's clothing shop, the supermarket (Melissa's till), outside again, home.
// Needs the v5.10 plugin (@jun, number positions, @move). Positions can be left / center / right or a number: -1 is left, 0 is the middle, 1 is right, -0.5 is halfway to the left.
// Portraits now use the game's own size and spot (v5.11): Annalie at the monitor is at the centre, Melissa at the till is 1.25. Jun stands to the left of them. Change the numbers to move people.
// Size (v5.12): @show Name position size, or @size Name 0.8 later. 1 = the size the game draws; Annalie_Outside is 0.72 here. Change it if she looks too big or small.
// Height and Jun's size (v5.13): @show Name position size height (height -0.7 = a bit lower), @move Name position height, @jun position size (1.4 = 40% bigger than in the room).
// Annalie uses her clothing-shop portrait in her shop and her "hangout" portrait (Annalie_Outside) in the supermarket and on the street.

# start
Narrator: Your phone buzzes on the desk. A message from Annalie, in all capital letters.
Narrator: "COME TO MY SHOP TODAY. BRING JUN. BRING SNACKS. DO NOT BRING THE LANDLORD."
{bot} [surprised]: Message analysis: three commands and one prohibition. Estimated urgency: eighty percent.
{player}: She always writes like that.
{bot} [happy]: Shall we go? I would like to see where Lady Annalie works.
? Go right now -> walk
? Look for snacks first -> snacks

# snacks
{player}: She said snacks. Let me check what we have.
Narrator: The cupboard holds half a packet of crackers and a very old can of soup.
{bot} [lecture]: According to my calculations, these crackers are still edible, and the soup is a historical artefact.
{player}: ...We will buy snacks on the way.
-> walk

# walk
@background outside
@jun -0.5 1.4
Narrator: The street is grey and busy. A paper bat drifts past, left over from someone's Halloween decoration.
{player}: I never leave the apartment this much. It feels like a field trip.
{bot} [happy]: I am logging this as a field trip. Please hold my hand if you get lost.
{player}: ...Jun.
{bot} [surprised]: It was a safety suggestion. I withdraw it.
Narrator: The shop appears at the end of the street, with a neon sign too bright for the day.
-> shop

# shop
@background clothier
@jun -0.95 1.4
@show Annalie center
Annalie [happy]: You came! Welcome to Lady Annalie's, home of drama and discounts!
Annalie [normal]: I need your honest opinion. I am making a window display for Halloween and I cannot choose.
? Say it should be a witch -> witch
? Say it should be a ghost -> ghost

# witch
{player}: A witch. The hat makes people look twice.
Annalie [surprised]: A hat! Of course! Nobody can resist a big dramatic hat!
Annalie [pervy]: And I just happen to own one.
{bot} [lecture]: Statistical note: witches are ninety percent hat. The remaining ten percent is drama.
Annalie [happy]: That is the best review I have ever received!
-> errand

# ghost
{player}: A ghost. You can do it with sheets, so it is cheap.
Annalie [sad]: Cheap...
Annalie [normal]: ...But true. This city has not been kind to my budget.
Annalie [pervy]: A ghost it is. A very expensive-looking cheap ghost.
{bot} [point]: A ghost made of sheets has a very high rating. Ghosts do not pay rent.
Annalie [happy]: Jun, you understand me better than anyone.
-> errand

# errand
Annalie [normal]: Okay, there is one tiny problem. I am out of candy for the display, and Melissa has the only good stock.
Annalie [happy]: Come with me to the supermarket! Please? She will give you a discount if you are nice.
{player}: She does not give discounts.
Annalie [pervy]: Then she will give us a very precise refusal. Let us go.
@hide Annalie
@background shop
@jun -1.1 1.4
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
Narrator: The supermarket hums. Fluorescent light, quiet music, a very straight queue.
Melissa [normal]: Good afternoon. I will serve you in the order of your arrival.
Annalie [happy][point]: Melissa! I brought friends!
Melissa [normal]: I can see that. Three visitors, one of whom is a robot. State your purchase.
Annalie [normal]: Candy for the shop window. Orange and black, if possible.
Melissa [normal]: Aisle four, shelf two. Four hundred and twelve grams. Please do not open the bags before payment.
Annalie [surprised]: I did not even say I would!
Melissa [normal]: Your hands said it for you.
{bot} [surprised]: Melissa is extremely accurate.
Melissa [normal]: Thank you. Also, I recovered my receipt. It has been filed under "Personal Opinions on Coupons."
? Ask to read it -> readit
? Buy the candy and go -> leave

# readit
{player}: Can I read the part about coupons?
Melissa [surprised]: No.
Melissa [normal]: Reading it would reveal my opinions. I will not discuss them with strangers.
Annalie [happy]: We are not strangers! We are the people who found your receipt!
Melissa [normal]: ...That is correct. I will say one sentence. "Coupons are a promise that the future will be cheaper."
Annalie [surprised]: That is the saddest and most beautiful thing I have ever heard.
-> leave

# leave
Melissa [normal]: Your total is thirty-one dollars and sixty cents. Please pay.
Annalie [sad]: ...Thirty-one sixty?
{player}: I will pay. Annalie, you are hopeless with money.
Annalie [happy][point]: That is why I have friends!
@hide Melissa
@move Annalie_Outside 0.5 0
@background outside
@jun -0.8 1.4
Narrator: Evening on the street. The neon sign of the shop reflects in the puddles.
{player}: Thanks for dragging us out, Annalie. This was... actually fun.
{bot} [happy]: I will mark it as a good memory. Category: field trip, candy, one witch hat.
Annalie [happy][down]: Come again! Next time I will bring cake and a fashion show.
Annalie [pervy][point]: And Jun, I expect you to model.
{bot} [surprised]: Model what?
Annalie [normal]: A hat. Just a hat. Do not panic.
@hide Annalie
@background room
Narrator: Back in the tiny apartment. Your own door. Your own very small couch.
{bot} [happy]: I enjoyed today. It was efficient, and nobody tried to evict us.
{player}: Do not say that out loud. He might be listening.
Narrator: Downstairs, a very loud voice sneezes.
Narrator: The end.
-> end
