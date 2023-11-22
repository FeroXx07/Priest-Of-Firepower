using System.IO;

namespace _Scripts.Interfaces
{
    public interface INetworkInput
    {
        string nameIdentifier { get;}
        void SendInputToServer();
        void ReceiveInputFromClient(BinaryReader reader);
        
        // void SendInputToClients();
        // void ReceiveInputsFromServer(BinaryReader reader);
    }
}