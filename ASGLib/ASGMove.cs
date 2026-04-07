using ASGLib.Chess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Superior Namespace : Abstract ASG Move Class
namespace ASGLib
{

    //Abstract Class for an Abstract Strategy Game Move
    public abstract class ASGMove<TMoveDTO> where TMoveDTO : Data.ASGMoveDTO
    {

        //Universal Properties : moveString is immutable, turnNum and turnPlayer are optional for declaration
        private String      moveString;
        protected int       turnNum;
        protected String    turnPlayer;

        //Constructor : Accessible by Child
        protected ASGMove(String moveString)
        {
            this.moveString = moveString;
            this.turnNum    = -1;
            this.turnPlayer = "";
        }

        //Accessor for MoveString : can be accessed by Move children
        protected String GetMove()
        {
            return moveString;
        }

        //Generic Json Serializer Contract
        internal abstract TMoveDTO ToDTO();
    }
}
