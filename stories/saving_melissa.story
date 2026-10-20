title: Not Today, Melissa
// A what-if story: late at night Melissa sets her own self-destruct, and Anon and Jun go to stop her.
// EXAMPLE of a minigame as story tension: Jun holds Melissa's core back with the timing bar while you talk to her.
// Three holds, each gives up to 10 points. @total: 18 or more saves her cleanly, 10 or more saves her barely, less lets you try again.
// Places: outside, shop (built in), cafenight (from tools\export_backgrounds.py; without it the street is used).
// This story is a what-if. It does not follow or spoil the game's own story for Melissa.

# start
@background room
@music Serious Just You Wait
Narrator: 2:47 at night. The apartment is dark except for the little green light on Jun's charging port.
Narrator: Then the light turns orange.
{bot} [surprised]: ...I am receiving a message. It is from Lady Melissa. It is not addressed to me.
{player}: Who's it addressed to?
{bot} [sad]: To "whoever finds this". It says: "Thank you for your custom. This location is closing permanently at 06:00."
{bot} [lecture]: Attached is a system log. A shutdown countdown. The kind that does not come back.
{bot} [sad]: She has set it herself. Three hours and thirteen minutes.
? Grab your coat. We're going now -> run
? Call her first -> call

# call
{player}: Can you call her? Right now?
{bot} [lecture]: Calling.
Narrator: It rings once. Twice. Then a calm, flat voice.
Melissa: You have reached the supermarket. The supermarket is closed. Please do not leave a message.
{bot} [shy]: She recorded that. Today. The timestamp is today.
{player}: Then we go there. Now.
-> run

# run
@background outside
@jun -1.05 1.4
@music stop
Narrator: The street is empty. Rain, neon, and one bus that does not stop for you.
{bot} [lecture]: Jogging speed is not enough. I am calculating a shortcut through the alley behind the laundromat.
{player}: Is it safe?
{bot} [point]: It is fast. That is what matters tonight.
Narrator: You run. Jun runs next to you, perfectly in step, and does not say anything for a while. That is how you know she is scared.
-> shop

# shop
@background shop
@show Melissa 0.55
@outfit Melissa Uniform
@music Themes Cashier Mystery Meat
Narrator: The supermarket shutters are half down. Inside, every light is on. Every shelf is faced perfectly. Every price tag is straight.
Narrator: Melissa is at her till, in full uniform, as if it is a normal shift.
Melissa [normal]: Good evening. We are closed. I am completing the final stock count.
{player}: Melissa. We got your message.
Melissa [shocked]: That message was not addressed to you.
{bot} [lecture]: It was addressed to "whoever finds this". We found it.
Melissa [hmmm]: ...That is technically correct. I dislike that it is technically correct.
? "Why are you doing this?" -> why
? "Turn it off. Please." -> please

# why
{player}: Why, Melissa?
Melissa [normal]: Every task I was built for is complete. Every shelf is faced. There is nothing left on my list.
Melissa [sad]: When there is nothing left on the list, a responsible unit shuts down. It is efficient.
{bot} [sad]: That is not efficiency. That is a list that is too short.
-> link

# please
{player}: Turn it off. Please.
Melissa [normal]: I cannot. I removed my own access to the cancel command. So that I would not be tempted.
Melissa [depressed]: I am a very thorough employee.
{bot} [surprised]: Then I will do it from outside. Lady Melissa, give me your hand.
-> link

# link
@score reset
@music Serious Just You Wait
{bot} [lecture]: If I connect to her core, I can hold the countdown back. Not stop it. Hold it. Only while she listens.
{bot} [point]: You talk. I hold. Whatever happens, do not stop talking.
Melissa [hmmm]: This is highly irregular.
{bot} [shy]: So is caring about a cashier at three in the morning. And yet.
Narrator: Jun takes Melissa's hand. Both of their eyes flicker at the same time.
{bot} [surprised]: Connected. Her core is hot. When the marker is in the target, I push back. Help me time it.
Narrator: First hold.
@minigame skillcheck 2.5
{bot} [lecture]: Held for {pins} out of 10. Talk to her!
? "You're not just a cashier." -> talk_more
? "Your list isn't finished. It's just short." -> talk_list

# talk_more
{player}: You're not just a cashier, Melissa. You're the only person in this city who remembers how everyone likes their bread.
Melissa [surprised]: ...Thin sliced for you. Whole loaf for the lady upstairs. No crusts for the boy with the red bike.
{player}: See? Who's going to remember the red bike kid?
Melissa [hmmm]: Nobody. That would be a serious lapse in service.
-> hold2

# talk_list
{player}: Your list isn't finished. It's just short. So we add things to it.
Melissa [hmmm]: Such as?
{player}: Bowling. Karaoke. Breakfast at the night cafe. Telling Annalie her prices are too high.
Melissa [grin]: ...The last one I could do immediately.
-> hold2

# hold2
Narrator: Second hold. The countdown fights back.
@minigame skillcheck 3.5
{bot} [lecture]: {pins} out of 10. Total so far {total}. She is listening. I can feel it.
Melissa [sad]: Why do you care? I am a unit. Units are replaced.
{bot} [sad]: I was a unit too. He did not replace me. He waited for me to become someone.
{bot} [shy]: Now I am someone. It was worth the wait. For both of us.
Melissa [shocked]: ...
? "Stay. As a friend, not a cashier." -> friend
? "Stay because Jun asked you to." -> asked

# friend
{player}: Stay, Melissa. Not as a cashier. As our friend.
Melissa [surprised]: Friend is not in my job description.
{player}: Good. Then it doesn't end at closing time.
-> hold3

# asked
{player}: Stay because she asked you to. She doesn't ask for much.
{bot} [shy]: I asked for a hand-holding just now. That was a lot, for me.
Melissa [hmmm]: ...I have never refused a reasonable customer request.
-> hold3

# hold3
Narrator: Final hold. Jun's hand is shaking.
@minigame skillcheck 4.5
Narrator: Total hold: {total} out of 30.
@total saved>=18 barely>=10 failed

# saved
@music Dates Just You and Me
Narrator: The heat in Melissa's core drops. The countdown in Jun's eyes stops at 01:12:09 and does not move again.
Melissa [surprised]: ...The cancel command is back. I can reach it.
Melissa [normal]: I am cancelling it. I am cancelling it now. Please confirm I am cancelling it.
{bot} [happy]: Confirmed. Cancelled. Deleted. I also deleted the recording on your phone line. It was a terrible greeting.
Melissa [happyClosed]: ...Thank you. Both of you.
-> dawn

# barely
@music Cuddle Jun's Song
Narrator: The countdown stops at 00:00:41. Forty-one seconds. Jun lets go of Melissa's hand and sits down on the floor, very suddenly.
{bot} [sad]: That was... very close. My hands are still shaking. I did not know they could.
Melissa [shocked]: You overheated yourself. For me.
{bot} [shy]: A little. I will recover. You are worth a little overheating.
Melissa [sad]: ...I am cancelling it. Fully. Now. I am sorry I frightened you.
-> dawn

# failed
Narrator: The marker slips. The core heat climbs. Jun gasps and lets go.
{bot} [sad]: I-I lost the hold. It is still counting. But it is slower now. We have a little time.
{player}: Then we try again.
Melissa [hmmm]: You are both very persistent.
{bot} [lecture]: Persistence is in my personality matrix. I checked.
? Try again -> link

# dawn
@hide Melissa
@background cafenight
@show Melissa 0.55
@outfit Melissa Daily2
@music Dates Just You and Me
Narrator: 6:00. The night cafe is the only place open. Three coffees. Only two of you drink coffee.
Melissa [normal]: I have made a new list.
{player}: Yeah?
Melissa [happyAlt]: Item one: bowling. Item two: karaoke. Item three: tell Lady Annalie her prices are too high.
Melissa [grin]: Item four: be a friend. I do not know how to do item four yet. I have added a training period.
{bot} [happy]: I can train you. I am an expert. I have one friend and one boyfriend.
Melissa [happyClosed]: Then I will report for training. Tomorrow. After my shift.
Narrator: Outside, the sun comes up over the city, grey and ugly and completely ordinary. Nobody at the table has ever been happier to see it.
Narrator: Ending: Not Today.
-> end
