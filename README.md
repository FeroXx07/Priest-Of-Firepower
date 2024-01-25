# Priest Of FirePower
A demo made for the Online &amp; Networks subject at the CITM-UPC. The objective of this project is to learn the internal structure of the networking layer in a video game.
From the sockets, authentication to network models, synchronization models, object replication and object serialization. So, the Unity's netcode is NOT used in this project, but a custom one created from scratch.

The chosen structure for this 2D top down shooter is "State Synchronization" for the synchronization model, "Client-Server" for the network model, and "Binary Serialization" for the object serialization.
How is this achieved?

- Authentication -- Ensure clients connected and authenticated can send the data to the server.
- Network Manager -- Connects the data generated by the network objects, behaviours and the sockets of the clients and servers.
- Network Object -- Sends transform replication every X ticks and performs interpolation.
- Network Behaviour -- Can send replication every X ticks (slower) and Y inputs every Y frames (faster).
- Replication manager -- Ensure consistency between objects that should have same state across the net.
- Replication Headers and Streams -- How replication data is ordered in a packet.
- Input Headers and Streams -- How input data is ordered in a packet.
- Packet, encompasses both replication and inputs -- Datagram that is sent through the sockets.
