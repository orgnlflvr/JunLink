title: Telling Big Daddy
// A what-if. Anon decides to tell Big Daddy about the organizations that are after Jun, including the one he "joined for a joke".
// Everything here is new writing; it only uses facts you already know: Big Daddy is a loud, overworked, generous businessman who knows Anon's secret,
// is part of the Binary Birthright Coalition (bot rights), once wanted to buy the bot, and adores his Little Angel.
// Needs the v5.14k plugin (@jun position and size). Big Daddy is the "BigDaddyHurt" portrait; his faces may be missing, so unknown [tags] are simply ignored.
// Positions are first guesses: set them on the stage page (http://127.0.0.1:8765/stage) and paste the @show / @jun lines over these.
color: Big Daddy = #8021ad

# start
@background outside
@music Themes BigDaddyHurt
@show BigDaddyHurt 0.8
@jun -0.5 1.7
Narrator: Big Daddy's car is parked across two spaces, engine still running. He is shouting into a phone about shipping containers and waves you over with his free hand.
Big Daddy [normal]: ...no, NOT the blue ones! The other blue ones! I will call you back. Little one! You wanted to talk? Make it quick, I have four meetings and a wife.
{player}: It is about the organizations. The ones that want Jun.
Big Daddy [surprised]: ...Oh.
Narrator: He turns the engine off. You have never seen his car quiet before.
Big Daddy [normal]: Okay. Talk. I will not look at my watch. Ask me again in an hour if I did.
{bot} [lecture]: Anon, I recommend caution. The probability that he already knows more than he says is high.
{player}: That is exactly why I am asking him.
? Tell him everything, Jun's code included -> everything
? Tell him only about the organizations -> orgs
? Ask what the Coalition would do first -> coalition

# orgs
{player}: There are people who would take Jun apart. Some of them are already gone. Some are not.
Big Daddy [normal]: The ones with the fancy long name that spells out something rude, yes. They fell apart. Good riddance.
{player}: And the others. The patriotic ones. They want an AI to run the country and follow the constitution so nobody has to.
Big Daddy [surprised]: That is a bad plan with a good slogan. Those are the worst kind.
-> joke

# everything
{player}: Jun is not a normal bot. Part of her code is something people want, or want destroyed. I have not told anyone, not even you.
Big Daddy [surprised]: ...
Big Daddy [normal]: I once asked if I could buy her.
{player}: You did.
Big Daddy [sad]: I was a fool. A rich fool, which is worse. If I had owned her, the paperwork would have had my name on it. Now it would have your name AND mine on a list.
{bot} [surprised]: He is more worried about the list than the offer. That is a good sign.
-> joke

# coalition
{player}: If I told you something that put you in danger, what would the Coalition do?
Big Daddy [normal]: Put you in a conference room with bad coffee, ask you to sign eleven documents, and then actually help. Why do you ask?
{player}: Because I think I need that help.
Big Daddy [normal]: ...Then tell me. All of it. Start with the part you think I will shout about.
-> joke

# joke
{player}: There is one more thing. And you will shout.
Big Daddy [normal]: I always shout. Try me.
{player}: I joined the patriotic organization. As a joke. I clicked a button on a website to see what would happen.
Big Daddy [surprised]: YOU WHAT?!
Narrator: A pigeon leaves the nearest lamp post. A passer-by crosses the road.
Big Daddy [surprised]: A JOKE?! Do you know how many people have lost a LOT of money clicking buttons on websites?! Do you know how many I have personally -
Big Daddy [normal]: ...okay. Okay. I am breathing. You told me. That is the correct thing to do. I am proud of you and I am also furious.
{bot} [surprised]: Both emotional states are logged.
Big Daddy [normal]: What did you give them? Name, address, bot serial number?
? Only a username and an email I never check -> small
? I used my real name. And my address. -> big

# small
{player}: A username and an old email. I never checked it again.
Big Daddy [happy]: THAT I can work with! Old email, throwaway name, no address. They have nothing. A shadow, a ghost, a pigeon.
Big Daddy [normal]: Close the account. Do not log in again. Do not even think about the website. I will have my lawyers erase the paper trail by dinner.
{bot} [lecture]: Deleting the account will not remove what they already logged. But it will stop them from adding more.
-> help

# big
{player}: ...My real name. And the address of the flat.
Big Daddy [surprised]: ...
Big Daddy [normal]: Well. Then they know where you live, and the next steps are going to be less fun.
Big Daddy [normal]: Listen carefully, little one. You do not panic, because Jun is watching your face. You do not move out tonight, because that tells them you noticed. You come to me tomorrow and we change a few things quietly.
{bot} [sad]: I am watching your face, Anon. Your face is panicking.
{player}: Sorry.
-> help

# help
Big Daddy [normal]: Right. This is what happens next, and I will say it once because my phone is vibrating and it is probably my wife.
Big Daddy [normal]: One. The Coalition will want to meet you. You are not joining. You are a guest. A guest with a name tag and bad coffee.
Big Daddy [normal]: Two. Nobody learns about Jun's code. Not from me, not from the lawyers, not from anyone. I never heard that part.
Big Daddy [normal]: Three. I am buying you a lock for your door. A very expensive lock. Do not argue, it is a business expense.
{player}: You can not expense a lock.
Big Daddy [happy]: Watch me.
Narrator: His phone buzzes again. He looks at it, and for a second, all of the shouting leaves his face.
-> wife

# wife
Big Daddy [sad]: It is my wife. She set up a thing. An intervention. About me working too much.
{player}: Are you going?
Big Daddy [sad]: I am going to be forty minutes late and I will say it was traffic. I always say it was traffic.
{bot} [lecture]: There is no traffic at this hour.
Big Daddy [surprised]: Do NOT tell her that.
Narrator: He pats your shoulder, too hard, the way he always does.
Big Daddy [normal]: Thank you for telling me. Nobody tells me anything unless they want money. You told me because you trusted me, and that is worth more than my entire shipping department.
? Say: "Then do not work yourself to pieces. Go to the intervention." -> intervention
? Say: "Then let us get started. I am staying with you." -> stay

# intervention
{player}: Go home early. Hug your wife. The containers can be blue or not blue without you.
Big Daddy [surprised]: ...You sound like my daughter.
Big Daddy [happy]: She told me the same thing last week. Her exact words were: "Daddy, the world will not end if you eat dinner sitting down."
Big Daddy [normal]: I will go. I will sit down. I will try not to look at my phone for one whole hour.
{bot} [happy]: I will monitor his phone usage. It will be unpleasant.
Narrator: Ending: Early Dinner. He kept his promise for fifty-one minutes. It was a personal best.
-> end

# stay
{player}: You help me with the lawyers, I help you not to drown. Deal?
Big Daddy [happy]: DEAL! Get in the car. Not in my car, it has a smell. In YOUR car. Follow me.
Narrator: You do not have a car. He remembers that halfway through the sentence and laughs so hard he has to lean on the bonnet.
Big Daddy [happy]: ...Right! Taxi! Taxi, on me!
Narrator: Ending: The Guest. A week later you sat in a conference room with a name tag that said GUEST and a cup of terrible coffee, and for the first time since the website, you were not alone with it.
-> end
