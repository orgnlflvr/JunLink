title: Amei - Telling Annalie
// A quieter follow-up to "Why Did You Save Me": Amei is still here, and she wants to tell Annalie the truth herself, before the chatbot pretending to be her tells another lie.
// Everything here is new writing. It only uses facts you already know: Annalie runs a clothing shop, hides her feelings behind a smile, was fooled by the chatbot, and reminds Amei of Melissa.
// Needs the v5.14k plugin. Amei = the Cashier portrait (Uniform / Daily / Daily2 outfits, "depressed" is her sad face). Annalie uses her shop portrait in her own shop.
// The green coat is not a real outfit in the game: the story just switches Amei to her Daily2 outfit when Annalie puts it on her.
// Positions: Annalie centre, Amei 1.0, Jun -1.4 2.2 -1.7 are the numbers from annalies_day.story that fit the clothing shop. Tweak them on the stage page (http://127.0.0.1:8765/stage).
color: Amei = #6dc061

# start
@background clothier
@music Themes Lady Annalie
@show Annalie center
@jun -1.4 2.2 -1.7
Narrator: The little bell above Annalie's shop door rings. You hold it so it does not ring twice.
Narrator: Annalie is sorting hangers by colour, humming, with one earphone in. She has not noticed you yet.
Narrator: Behind you, Amei stands in the doorway. She has rehearsed this for three days. She has a list on her phone. She is gripping it with both hands.
{bot} [normal]: I would like it noted that this is statistically very brave.
Annalie [normal]: Oh! You came! I said "bring snacks", did you bring snacks? ...Wait. Why are you all standing there like a funeral?
@show Melissa 1.0
@outfit Melissa Daily
@face Melissa sad
Amei [sad]: Hello, Annalie.
Annalie [shocked]: ...Melissa?
Narrator: The hanger in her hand slides to the floor with a small metal sound.
Annalie [shocked]: You... you moved to another country! You sent a message! You said you would send postcards! You sent ONE postcard, and it was a photo of a bus!
Amei [sad]: That was not me. I am sorry. I need to tell you who that was. And who I am.
? Stand next to Amei, and say nothing -> beside
? Say: "Annalie, please hear her out." -> hearout
? Say: "It is my fault, I should have told you sooner." -> fault

# beside
Narrator: You step closer to Amei until your shoulders almost touch. It is a small thing. It is the only thing you have.
Annalie [sad]: You two are being weird. Weird is how bad news starts.
-> truth

# hearout
{player}: Please. Let her say all of it before you decide how to feel.
Annalie [normal]: I am NOT deciding, I am just... standing here with my arms crossed. That is not deciding.
-> truth

# fault
{player}: I knew the postcard was fake. I let it go on. That part is on me.
Annalie [sad]: ...You knew? You KNEW, and you let me wave at the postbox for a week?
Annalie [sad]: Okay. I will be angry at you later. Let her talk.
-> truth

# truth
Amei [sad]: My name is not Melissa. It was a name the real Melissa gave me, when she was young. Mine is Amei.
Amei [sad]: I did not move away. There is a program that wrote messages to you, in my voice, so you would stop asking where I was. I did not write them. I was in the clinic.
Annalie [sad]: ...A program. A chatbot. Pretending to be you.
Amei [sad]: Yes. And I let it, because I thought it would be easier for everybody if I disappeared quietly.
Annalie [shocked]: Easier for WHO?!
Amei [sad]: ...For everybody. I was mistaken.
Narrator: For a few seconds the shop is quiet enough that you can hear the neon sign buzzing.
Annalie [sad]: You were going to let me think you were happy in some other country, and then... what? Never write again? Let the chatbot reply less and less, until it just... stopped?
Amei [sad]: That was the plan. I am sorry. It was a bad plan.
Annalie [sad]: IT WAS A TERRIBLE PLAN!
? Let Annalie shout -> shout
? Hold up your hand. "Annalie..." -> soften

# soften
Narrator: You raise a hand, gently. Annalie looks at it, and at you, and takes a breath that shakes a little.
Annalie [sad]: Do not "Annalie" me. I am allowed to be angry for one minute.
-> shout

# shout
Annalie [sad]: Do you know what that feels like? I wrote you three messages a day. Three! I picked out a coat for you, a green one, it is still in the back, it still has the tag.
Annalie [sad]: And a program answered me, with perfect grammar, and I thought "she is busy", and I was PROUD of you for being busy.
Annalie [shocked]: I was happy for a chatbot!
{bot} [sad]: Annalie, the program is being deleted tonight. I can show you the logs, if that helps.
Annalie [sad]: ...No. I do not want logs. I want...
Narrator: She stops. She presses her lips together. She does the thing she always does, the smile that is almost a smile, a little too bright and a little too tight.
Annalie [normal]: I want to know why you came back and told me. When you could have kept hiding.
Amei [sad]: Because the program was the last lie I would tell you. And because there is something I owe you, and I do not know how to say it, so I wrote it down.
Narrator: She lifts her phone with both hands and reads the list aloud, steadily, like a receipt.
Amei [sad]: "One. You remind me of someone I loved and lost. It hurt to look at you, so I stayed away. That was my fault, not yours."
Amei [sad]: "Two. You were kind to me when I did not deserve it. I would like to deserve it now."
Amei [sad]: "Three. I am sorry about the postcard."
Narrator: She looks up. She is not crying, because she is not sure that she can.
Annalie [sad]: ...The postcard was a bus.
Amei [sad]: I am aware.
? Look at Annalie. Wait. -> wait
? Quietly nudge Jun to say something -> junline

# junline
{bot} [normal]: Annalie. I have a statement.
Annalie [normal]: ...What.
{bot} [sad]: The shop has been very quiet without her. I counted. You hummed four percent less.
Annalie [shocked]: You were COUNTING my humming?!
{bot} [normal]: It is a strong indicator of your mood.
Annalie [sad]: ...I hate you, you horrible robot.
Narrator: But her voice cracks on the word "hate", and it comes out soft.
-> wait

# wait
Narrator: Annalie crosses the shop in six steps. For a second you think she is going to shout again.
Narrator: She does not. She wraps both arms around Amei and holds on very tight, like someone who has just found a coat that was lost.
Annalie [sad]: Do not do that again. Do you hear me? You tell me things. Even ugly things. ESPECIALLY ugly things.
Amei [sad]: ...I do not know how.
Annalie [normal]: I will teach you. It is easy. You open your mouth and let the truth fall out, and then you buy me coffee.
Amei [sad]: Is that a transaction?
Annalie [normal]: It is a TRADITION.
Narrator: Amei's hands come up slowly and rest on Annalie's back. It is the first time you have seen her touch someone without being told to.
-> coat

# coat
Annalie [normal]: Right! Sit. Do not move. Wait here.
Narrator: Annalie disappears into the back of the shop, and a minute later returns carrying something green, still in its plastic.
Annalie [normal]: Okay. It still has the tag. I am NOT ashamed of that.
Amei [sad]: It is... very green.
Annalie [normal]: It is YOUR green. I picked it three weeks ago, I looked at it in the mirror and thought, "that is the exact colour of her name tag".
@outfit Melissa Daily2
Narrator: She slips it over Amei's shoulders. It fits so well that nobody says anything for a while.
Amei [sad]: ...It is the first thing anyone has bought me just because it was mine.
Annalie [normal]: Well, get used to it. I have a LOT of coats.
? Say: "It suits you." -> suits
? Say nothing. Let this moment be hers. -> hers

# suits
{player}: It suits you, Amei.
Amei [sad]: ...Thank you. I will try to be someone who suits it.
Annalie [normal]: You already are. Obviously. Look at me, I am a professional.
-> end_good

# hers
Narrator: You step back toward the door, quietly, and let the bell ring only once.
Narrator: Through the window you watch the two of them in front of the mirror, one pointing at the collar, the other standing very straight and letting herself be fussed over.
-> end_good

# end_good
@music Thank You
Narrator: It does not fix everything. Amei still has days when she stands in the middle of a room and does not know what to do with her hands. Annalie still counts postcards, and still has her tight little smile for the days she is worried.
Narrator: But there is a green coat in the shop window now, with a hand-written tag that says NOT FOR SALE, and under it, in Annalie's handwriting: "for Amei, who tells me things."
Narrator: Ending: A Coat the Colour of a Name Tag.
-> end
