using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMS.Services
{
    public interface IAppDateTimeService
    {
        DateTime GetCurrentDateTime();
        void SetCurrentDateTime(string inMode, DateTime inMockUpCurrentDateTime);


    }
    public class AppDateTimeService: IAppDateTimeService
    {
       DateTime _mockDateTime;
       private string _mode { get; set; }
       public AppDateTimeService(string inMode,DateTime inMockUpCurrentDateTime){
                _mode = inMode;
                //https://www.dotnetperls.com/datetime-minvalue
                _mockDateTime = inMockUpCurrentDateTime;      
          
       }//End constructor
       public DateTime GetCurrentDateTime(){
            if (_mode == "mock")
            {
                return _mockDateTime;
            }else{
            //If the _mode is "actual", return current system date time
                return DateTime.Now;
            }
       }
        public void SetCurrentDateTime(string inMode, DateTime inMockUpCurrentDateTime)
        {  //Note that, the inMockUpCurrentDateTime although has incoming values but the
            //_mode member variable's value will decide whether to use it in the GetCurrentDateTime method.
            //This AppDateTimeService class was designed in a very rush manner.
            _mode = inMode;
            //https://www.dotnetperls.com/datetime-minvalue
            _mockDateTime = inMockUpCurrentDateTime;
            return;
        }
    }
}
