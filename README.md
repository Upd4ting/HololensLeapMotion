# HololensLeapMotion
The most effective way to implement the LeapMotion on the Hololens.

This is using these library:

-	https://github.com/Upd4ting/HololensTemplate
- https://github.com/ZhengyiLuo/LeapMotion_Hololens_Asset/ (We used this base of code that we changed a little)

# Contributing

You can contribute by forking and doing pull request

# Concept 

The LeapMotion directly isn't compatible with UWP. The only solution then is to stream the data of the leapmotion to the Hololens.
The LeapMotion provides a websocket that we can use to retrieve these data. The library of Zhengyilua above is doing this concept but I made a lot of improvement here. 

You have now a middleware server between the WebSocket and the Hololens. The Hololens connect to the middleware and can tell him how much frame rate he wants. So with that you are able to control the frame rate in an easier and cleaner way. 

I also fixed some error that was causing bugs and performance issues so all run now smoother.

# Team

The developement team of this project is composed of Antony Rizzitelly and Pierre Delaisse (https://www.linkedin.com/in/pierredelaisse). 
Pierre updated the code from https://github.com/ZhengyiLuo/LeapMotion_Hololens_Asset/ to the current version of Hololens and Leap Motion. 
Antony created the websocket to vary the framerate sent through the network. 
