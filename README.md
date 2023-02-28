﻿# TuioNet

## Documentation
A brief overview over the most important classes and how to use them.

### TuioReceiver.cs
An abstract base class for different connection types for receiving messages from a TUIO source. Currently there
are two types of receivers implemented: ```UdpTuioReceiver.cs``` and ```WebsockeTuioReceiver.cs```

#### Add/Remove Message Listeners
The TUIO Specification ([1.1](http://tuio.org/?specification) and [2.0](http://www.tuio.org/?tuio20)) defines different kinds of
TUIO message profiles:</br>

TUIO 1.1 Examples:
```
/tuio/2Dobj set s i x y a X Y A m r
/tuio/2Dcur set s x y X Y m
/tuio/2Dblb set s x y a w h f X Y A m r
```

TUIO 2.0 Examples:
```
/tuio2/tok s_id tu_id c_id x_pos y_pos angle [x_vel y_vel m_acc r_vel r_acc]
/tuio2/ptr s_id tu_id c_id x_pos y_pos angle shear radius press [x_vel y_vel p_vel m_acc p_acc]
/tuio2/bnd s_id x_pos y_pos angle width height area [x_vel y_vel a_vel m_acc r_acc]
/tuio2/sym s_id tu_id c_id t_des data
```

Tuio clients can listen to specific messages and register callback methods for them. In the current implementation
the TUIO clients listen to the following message profiles.

Tuio11Client.cs:
```csharp
public Tuio11Client(TuioReceiver tuioReceiver)  
{  
    _tuioReceiver = tuioReceiver;  
    _tuioReceiver.AddMessageListener("/tuio/2Dobj", On2Dobj);  
    _tuioReceiver.AddMessageListener("/tuio/2Dcur", On2Dcur);  
    _tuioReceiver.AddMessageListener("/tuio/2Dblb", On2Dblb);  
}
```

Tuio20Client.cs
```csharp
public Tuio20Client(TuioReceiver tuioReceiver)  
{  
    _tuioReceiver = tuioReceiver;  
    _tuioReceiver.AddMessageListener("/tuio2/frm", OnFrm);  
    _tuioReceiver.AddMessageListener("/tuio2/alv", OnAlv);  
    _tuioReceiver.AddMessageListener("/tuio2/tok", OnOther);  
    _tuioReceiver.AddMessageListener("/tuio2/ptr", OnOther);  
    _tuioReceiver.AddMessageListener("/tuio2/bnd", OnOther);  
    _tuioReceiver.AddMessageListener("/tuio2/sym", OnOther);  
}
```

#### Process Messages
The TuioReceiver class is responsible for processing the TUIO messages which it receives from the sender. 
The ```OnBuffer``` method is called everytime a new message gets received and it puts the new message in a message
queue. The ```ProcessMessages``` method is looking for new messages in the message queue and calls the associated
callback methods the message listeners registered to them. If the ```isAutoProcess``` flag is set to ```true``` the ```ProcessMessages```
method is called automatically. Otherwise it needs to be called manually.

### Tuio Clients



## Demo Console Application
