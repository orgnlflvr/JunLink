title: Unscheduled
// Jun plans a perfect day off down to the minute. The day has other plans.
// Needs plugin v5.14ze and the game's pictures saved by tools\export_backgrounds.py (plaza, amusementpark1, amusementpark3, aquariuminside, aquariumnightlight, cafenight, stargazing2).
// Positions copied from bowling_night.story: Jun -0.8 size 1.4, Annalie_Outside 0.1, Melissa 1.25 in her Daily2 outfit. Tune them in the stage page if anyone overlaps.
// Three endings: "Stars" (the warm one), "Four Chairs" (friends), "Twenty-Three Minutes Late" (bittersweet).

# start
@music Daybreak With You
{bot} [happy]: Good morning. I have prepared something for our day off.
Narrator: Jun holds out a sheet of paper. It is printed on both sides, in very small letters.
{player}: ...Is that a schedule?
{bot} [lecture]: It is Operation Perfect Day. Fourteen activities, timed to the minute, including travel, snacks and two scheduled moments of spontaneity.
{player}: Scheduled spontaneity.
{bot} [point]: Item nine. Fifteen fifty to sixteen oh five. You may be as spontaneous as you like during that window.
{bot} [shy]: I calculated that we do not get many free days. I did not want to waste a single minute of this one.
? Promise to follow the schedule exactly -> strict
? Gently take the paper and fold it into your pocket -> pocket
? Ask what happens if something goes wrong -> wrong

# strict
{player}: Then we follow it exactly. Every minute.
{bot} [happy]: Thank you. Departure in four minutes. Please put on your shoes in the order left, right.
{player}: Why that order?
{bot} [lecture]: It saves zero point three seconds. I have run the numbers.
-> plaza

# pocket
{player}: How about I carry it? Just in case we need it.
{bot} [surprised]: You want to keep it close to you?
{bot} [happy]: That is acceptable. The plan will be near your heart. Statistically, a very safe place.
-> plaza

# wrong
{player}: And if something goes wrong?
{bot} [lecture]: There is a backup schedule on the other side of the paper.
{player}: And if the backup goes wrong?
{bot} [surprised]: ...
{bot} [sad]: I did not print a third side. Paper only has two.
{player}: Then I guess we would have to improvise.
{bot} [shy]: Improvise. I will try not to think about that word.
-> plaza

# plaza
@background plaza
@jun -0.8 1.4
@music Simulated Happiness
Narrator: The plaza is crowded with weekend people. A fountain coughs. Somebody sells balloons that are mostly holding their shape.
{bot} [point]: Item one: walk across the plaza. Estimated time, three minutes and ten seconds. We are on schedule.
Narrator: A very loud voice cuts across the square.
Annalie: SPECIAL OFFER! Hats that make you look TALLER! And more MYSTERIOUS!
@show Annalie_Outside 0.1 0.72 -0.7
Annalie [happy][point]: You two! My favourite customers! You look like people who need a mysterious hat!
{bot} [lecture]: We are on item one. Hats are not on the schedule until never.
Annalie [surprised]: A schedule? On a day off? Let me see that.
Annalie [sad]: Oh no. Oh no, it is double-sided.
Annalie [pervy]: You know what this day needs? Me. I close the stand early. Nobody buys hats before noon anyway.
{bot} [surprised]: Adding one Annalie to the plan will cost approximately forty minutes of shouting.
Annalie [happy]: Thirty-five if you are lucky!
? Invite her along -> annalie_joins
? Tell her you will catch up later -> annalie_later

# annalie_joins
{player}: Come with us. The more the better.
Annalie [happy][point]: Yes! Where are we going?
{bot} [lecture]: Item two. The amusement park. Arrival at ten fifteen, one ride, one pretzel, departure at eleven ten.
Annalie [pervy]: One pretzel. For three people. I love how dangerous this is.
-> park

# annalie_later
{player}: We are on a tight plan. Catch up with us later?
Annalie [sad]: Fine. Abandon me to my hats.
Annalie [pervy]: But I know where the amusement park is. And I know you will need me.
@hide Annalie
Narrator: She waves you off with a hat in each hand. You do not see her follow you. You will.
-> park

# park
@hide all
@background amusementpark1
@jun -0.8 1.4
@music Our Time
Narrator: The amusement park is bright paint over old metal. Music plays from somewhere above the rides, slightly too slow.
{bot} [happy]: Item two. We are three minutes early. I am experiencing a feeling I will label "pride."
Narrator: A sign hangs on the gate of the roller coaster: CLOSED FOR REPAIRS. Under it, in smaller letters: PROBABLY UNTIL TUESDAY.
{bot} [surprised]: ...
{bot} [sad]: The schedule did not account for Tuesday.
@show Annalie_Outside 0.1 0.72 -0.7
Annalie [happy][point]: Found you! I followed the smell of panic.
@show Melissa 1.25
@outfit Melissa Daily2
Melissa [normal]: Good morning. The coaster has been broken for nine days. The sign changes its estimate every morning. I keep a record.
Annalie [surprised]: Melissa? On a day off? Without a till?
Melissa [happyAlt]: I come here on Sundays. I time the queues. It is relaxing.
{bot} [sad]: Our plan had one ride in it. That ride no longer exists.
Melissa [hmmm]: There are two alternatives. The Ferris wheel has a queue of eleven minutes. The aquarium next door has no queue, but it is dark inside and the fish do not care about schedules.
{bot} [lecture]: I must choose the option with the smallest deviation from the plan.
{player}: Or we could just pick the one that sounds fun.
{bot} [surprised]: That is not a recognised method.
? The Ferris wheel -> wheel
? The aquarium -> aquarium

# wheel
@background amusementpark3
@jun -0.8 1.4
Narrator: The four of you squeeze into a cabin built for three. It rises slowly over the park.
Annalie [happy]: Look! You can see my stand from here! Nobody is stealing the hats. That is somehow worse.
Melissa [happyAlt]: The cabin moves at zero point three metres per second. We will reach the top in four minutes.
Narrator: You reach the top. Then the wheel stops. Then it stays stopped.
Annalie [surprised]: Why are we not moving? Why is nobody moving? Melissa, why is the number not moving?
Melissa [shocked]: ...I do not have a number for this.
{bot} [lecture]: Remain calm. The probability of the wheel staying stuck forever is very low.
Annalie [sad]: How low?
{bot} [sad]: I do not know. That is the first time I have said that today.
Narrator: The cabin sways gently. The city spreads out below you, grey and gold, every roof catching the light.
{bot} [surprised]: ...Oh.
{player}: What is it?
{bot} [shy]: This view was not on the schedule. It is better than everything that was.
Narrator: After seven minutes the wheel shudders and moves again. Annalie cheers as if you have survived a war. Melissa writes something in a small notebook.
Melissa [grin]: New record. Seven minutes stuck. Nobody cried.
Annalie [pervy]: I cried a little on the inside. It counts.
-> rain

# aquarium
@background aquariuminside
@jun -0.8 1.4
@music Candlelight
Narrator: The aquarium is cool and blue and quiet. Light moves across the floor in slow waves. Even Annalie lowers her voice.
Annalie [normal]: It is like being inside a very expensive dress.
@hide Annalie
@hide Melissa
@background aquariumnightlight
@jun -0.8 1.4
Narrator: In the last room there is a tank of jellyfish. They drift without hurrying, lit from below, going nowhere in particular.
{bot} [surprised]: They have no plan.
{player}: Nope.
{bot} [lecture]: They have no brain either. But they look... content.
Narrator: Jun stands very close to the glass. Her reflection floats among them.
{bot} [shy]: I wanted today to be perfect because I wanted to prove I could make you happy without making any mistakes.
{player}: You do not need a schedule for that, Jun.
{bot} [sad]: Then what do I need?
{player}: Just this. Standing here. Being here.
{bot} [happy]: ...I will try to be more like a jellyfish. Without the part where I have no brain.
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily2
Annalie [happy][point]: There you are! Melissa found the gift shop and she is reading the price tags out loud!
Melissa [happyClosed]: Every one of them is too high. I enjoyed it very much.
-> rain

# rain
@hide all
@background plaza
@jun -0.8 1.4
@music Rainy Day
Narrator: By late afternoon the sky has turned the colour of a dirty spoon. The first drops fall. Then all of them fall at once.
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily2
Annalie [surprised]: My hair! My hats! In that order!
Melissa [hmmm]: The forecast said fourteen percent chance of rain. The forecast is a liar.
{bot} [sad]: Item twelve was the planetarium. Item thirteen was stargazing on the hill. Item fourteen was home by twenty-one hundred.
{bot} [sad]: The planetarium closed early for a staff meeting. You cannot see stars through rain. Operation Perfect Day has failed.
Narrator: She looks down at her hands as if the plan was something she had been holding and dropped.
? Pull out the folded schedule and tear it in half -> tear
? Say "then we follow the backup plan" -> backup
? Say nothing. Hold the umbrella over her instead -> umbrella

# tear
{player}: Jun. Watch this.
Narrator: You tear the schedule in half. Then in quarters. Annalie gasps as if you have torn a painting.
{bot} [surprised]: You... destroyed it.
{player}: It did its job. It got us out of the apartment. Now the day is ours.
{bot} [shy]: ...Then the day is ours.
-> cafe

# backup
{player}: Then we use the backup plan. The one on the other side.
{bot} [surprised]: You read the back?
{player}: I read all of it while you were not looking.
{bot} [lecture]: The backup plan is "find somewhere warm and wait." It was the plan I was least proud of.
Melissa [happyAlt]: It is a very good plan. I have a cafe in mind.
-> cafe

# umbrella
Narrator: You do not say anything. You just open the umbrella and hold it over her, so most of it covers her and most of you gets wet.
{bot} [surprised]: You are getting wet. That was not efficient.
{player}: I know.
{bot} [shy]: ...I am going to remember this more than anything on the schedule.
Annalie [sad]: Excuse me. Some of us also have hair.
Melissa [happyAlt]: There is a cafe two minutes away. I counted.
-> cafe

# cafe
@hide all
@background cafenight
@jun -0.8 1.4
@music She's Real to Me
Narrator: The cafe is small and warm. The windows are fogged, the chairs do not match, and the coffee smells like something burned a long time ago and is still proud of it.
@show Annalie_Outside 0.1 0.72 -0.7
@show Melissa 1.25
@outfit Melissa Daily2
Annalie [happy]: Four hot chocolates! One with extra drama!
Melissa [grin]: There is no drama on the menu. I checked.
Annalie [pervy]: That is because I bring my own.
Narrator: Steam rises from the cups. Outside, the rain turns the street lights into long gold lines.
{bot} [lecture]: I would like to say something. I have prepared a short speech.
Annalie [surprised]: Is it on the schedule?
{bot} [happy]: No. It is unscheduled.
{bot} [shy]: Today nothing happened the way I planned. The ride was broken. The wheel stopped. It rained. And it was the best day I have recorded.
{bot} [lecture]: I do not understand the maths. I would like to keep not understanding it.
Melissa [happyClosed]: That is the most reasonable thing anyone has said today.
Annalie [sad]: Stop it. My makeup was not built for feelings.
Narrator: The rain softens. Through a clear patch in the window, the clouds are pulling apart.
? Take Jun to the hill to see if the stars came out -> stars
? Stay here with everyone until the cafe closes -> four_chairs
? Check the time and head home -> late

# stars
@hide all
@background stargazing2
@jun -0.8 1.4
@music Jun's Song
Narrator: The hill is wet and cold and nearly empty. Above it, the clouds have opened like curtains, and the sky is full.
{bot} [surprised]: Item thirteen. Stargazing on the hill.
{bot} [happy]: We are three hours and twelve minutes late.
{player}: Does it matter?
{bot} [shy]: No. I think this is the correct time. The schedule was wrong.
Narrator: You sit together on the damp grass. Jun points out constellations, then gets one wrong, then decides she likes her version better.
{bot} [point]: That one is called "the broken roller coaster." That one is "Annalie's hat." That small one is us.
{player}: Which one?
{bot} [shy]: The two close together. Slightly out of order. Not exactly where they are supposed to be.
Narrator: She leans her head on your shoulder. For once, she does not calculate anything.
Narrator: Ending: Stars.
-> end

# four_chairs
Narrator: You stay. The rain comes and goes. Annalie tells the story of the Ferris wheel three times, and each time it gets longer and more dangerous.
Annalie [pervy]: And then I held the entire cabin together with my bare hands!
Melissa [grin]: You held my sleeve and said "I am too young to be a ghost."
Annalie [surprised]: I said it with dignity!
{bot} [happy]: I have started a new document. It is called "Next Day Off." It has only one item.
Melissa [happyAlt]: Read it.
{bot} [lecture]: Item one. All four of us. No schedule.
Annalie [happy][point]: I accept! I will bring hats!
Melissa [happy]: I will bring a timer. I will not use it. I will just hold it.
Narrator: When the cafe finally closes, the owner has to turn the lights off twice before anyone notices.
Narrator: Ending: Four Chairs.
-> end

# late
{player}: It is almost nine. We said home by twenty-one hundred.
{bot} [surprised]: You remembered item fourteen.
@hide all
@background room
@music Take a deep breath
Narrator: You get home at nine twenty-three. Your shoes are wet. Your socks are wetter.
{bot} [lecture]: We are twenty-three minutes late. Operation Perfect Day ends with a failure rate of ninety-three percent.
{player}: Was it a bad day, then?
{bot} [sad]: ...
{bot} [shy]: No. The numbers say it was a bad day. Everything else says it was a good one. I do not know which to believe.
Narrator: She picks up a pen, finds the last line of the schedule, and crosses it out. Underneath she writes, slowly, in her small careful letters:
{bot} [happy]: "Item fifteen. Unscheduled. Tomorrow."
Narrator: Ending: Twenty-Three Minutes Late.
-> end
