title: Saving Amei
// A story with three endings. The supermarket is going to retire Melissa (Amei) at closing time. Can you save her?
// Needs the v5.14c plugin. The positions are first guesses: open the stage page (http://127.0.0.1:8765/stage), set up each scene, and paste the lines over the @jun / @show lines here.
// Melissa and Amei are the same portrait (the game calls her "Cashier"). Jun is placed with  @jun position size height.
color: Amei = #6dc061
color: Shop speaker = #ffe300

# start
Narrator: Evening. The cupboard is empty again, and your phone buzzes with a notice from the supermarket app.
Narrator: "STAFF NOTICE: Cashier 07 (Melissa) will be retired at closing time. Replacement: TILL-BOT 3000. Thank you for shopping efficiently."
{bot} [surprised]: Message analysis: they are retiring a person like an old appliance. Estimated moral concern: ninety-four percent.
{player}: That is Melissa. She has worked there forever.
{bot} [lecture]: She counts change faster than I do. I am slightly jealous, and I do not enjoy being jealous.
? Go to the supermarket right now -> walk
? Ask Jun how much time we have -> time

# time
{bot} [lecture]: Closing time is in twenty-two minutes. The walk takes eight. That leaves fourteen minutes of heroism.
{player}: Fourteen is enough.
-> walk

# walk
@background outside
@jun -0.5 1.7
Narrator: The street is grey. The supermarket sign flickers like it is nervous.
{player}: What do we even do? Shout at a building?
{bot} [happy]: A plan: we enter, we observe, we improvise. Improvisation fails ninety percent of the time, but it is very exciting.
-> arrive

# arrive
@background shop
@jun -0.6 1.7
@show Melissa 1.25
Narrator: The supermarket is almost empty. Fluorescent light, quiet music, a queue with nobody in it.
Melissa [normal]: Welcome. I will serve you in the order of your arrival. You are the order.
Shop speaker: Attention, valued customers. An efficiency review is in progress. Please do not distract the cashier.
Melissa [normal]: That is for me. Ignore it. Do you have a basket?
{player}: Melissa... is it true? They are retiring you tonight?
Melissa [surprised]: ...You read the notice.
Melissa [normal]: Yes. Cashier 07 is slower than a Till-Bot by point four seconds. I checked the maths myself. It is correct.
{bot} [sad]: Point four seconds. That is a very small reason.
Melissa [normal]: It is a reason. The rules are written down. Rules are the only thing holding this place together.
? Argue with the shop speaker -> shout
? Buy as much as possible to raise her numbers -> buy
? Ask Melissa what she wants -> want

# shout
{player}: Hey! You up there! She is a person, not a point four second problem!
Shop speaker: Customer is raising their voice. Please lower it, or you will be removed.
Melissa [surprised]: Please stop. If you are banned, I cannot serve you, and that is worse for my numbers.
{bot} [surprised]: The speaker has no feelings, so it cannot be embarrassed. This is a flaw in the plan.
Narrator: A small red clock on the wall says there are three minutes left.
-> late

# buy
{player}: Jun, grab a basket. We are buying everything.
{bot} [happy]: Understood. I will register enthusiasm and a lot of batteries.
Narrator: You pile the counter with candy, batteries, soap and one inflatable ghost. Melissa scans each item without a single mistake.
Melissa [surprised]: This is... a lot of customers' worth of items. My numbers are improving.
Shop speaker: Cashier 07 efficiency: seventy-one percent. Till-Bot 3000 efficiency: ninety-nine percent. Review continues.
{bot} [lecture]: We have raised her score by a few points. It is a nice try, but the target is a machine.
Narrator: The clock on the wall says there are three minutes left.
? Keep buying until closing -> end_delay
? Ask Melissa what she wants -> want

# late
Narrator: Two minutes left. The Till-Bot 3000 is already warming up at the next register.
? Ask Melissa what she wants -> want
? Accept it and say goodbye -> end_sad

# want
{player}: Forget the rules. What do you want, Melissa?
Melissa [surprised]: ...Nobody has asked me that on shift. Employees are not asked what they want.
Melissa [normal]: I like the quiet after closing. I like counting coins. I like it when a customer says thank you and means it.
Melissa [sad]: I do not like being a number. Cashier 07. My name is Amei.
Amei [normal]: Melissa is what the badge says. Amei is what my mother called me. Nobody has said it in years.
{bot} [happy]: Amei. Logged. Saved under my favourite category: people worth saving.
{bot} [lecture]: Also, I read the staff contract while she was speaking. Rule fourteen: an employee who resigns before closing cannot be retired. A retirement can only happen to an employee.
{player}: So if she quits first, there is nothing for the Till-Bot to replace.
Amei [surprised]: Quit. I have never quit anything. I have never even been late.
? Offer her the very small couch in your apartment -> good
? Tell her to fight the review -> fight

# fight
{player}: Then fight it. Tell them the review is wrong.
Amei [normal]: Fighting is not in my job description. Counting is. Standing at my till is.
{bot} [point]: Then standing is the part to change. Amei, you are allowed to leave.
Amei [sad]: ...Allowed.
Amei [normal]: Nobody has said that word to me before.
-> good

# good
Amei [normal]: Please give me a moment.
Narrator: She takes a receipt from the roll and writes on the back of it, very neatly: "I resign, effective immediately. Thank you for shopping."
Narrator: She puts the badge on the counter, exactly in the middle.
Shop speaker: Cashier 07 has left the system. Efficiency review: cancelled. Thank you for shopping efficiently.
{bot} [surprised]: The speaker is confused. Excellent. I will treasure this moment.
@hide Melissa
@background outside
@jun -0.8 1.7
@show Melissa 0.6
Narrator: The street is the same grey as before, but it sounds different. She walks a little more slowly than a cashier does.
Amei [happy]: The air outside has no price on it. That is strange.
{player}: It is free. Mostly.
Amei [normal]: Then I will take all of it. And I will take the couch, if the offer is still open.
{bot} [happy]: It is open. It is very small. I have measured it. You will have to sit on the left.
@hide Melissa
@background room
Narrator: Back at the tiny apartment, the couch has a second person on it.
Amei [normal]: Your cupboard has half a packet of crackers. I will make an inventory.
Narrator: Downstairs, a very loud voice sneezes.
Narrator: Ending 3 of 3: Amei. You saved her.
-> end

# end_delay
Narrator: You keep buying until the clock hits nine. The counter is a mountain of candy, batteries and one very proud inflatable ghost.
Shop speaker: Review extended by twenty-four hours due to customer volume. Thank you for shopping efficiently.
Melissa [surprised]: I have one more day.
Melissa [normal]: Come back tomorrow. Please bring fewer items, and the same kind of customers.
{bot} [lecture]: We have bought her a day. A day is not a plan, but it is a very good start.
Narrator: Ending 2 of 3: One more day. There is still a way to do better. Try asking her what she wants.
-> end

# end_sad
{player}: We will... come back tomorrow.
Melissa [normal]: There is no tomorrow for Cashier 07. But thank you for saying it.
Narrator: The doors lock at nine. A new till glows to life with a cheerful chime.
Shop speaker: Thank you for shopping efficiently.
Melissa [sad]: Your total is zero. You have bought nothing. That is correct.
@hide Melissa
@background room
Narrator: Back in the tiny apartment, the quiet feels heavier than usual.
{bot} [sad]: I did not ask her name. I would like to ask it next time.
Narrator: Ending 1 of 3: Retired. There is still a way to save her. Try asking her what she wants.
-> end
