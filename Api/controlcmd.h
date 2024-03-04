#pragma once

#define BROADCAST_MAX  20

#define API_TIMEOUT_INFINITE 0
#define API_TIMEOUT_DEFAULT 1000
//2022.09.14 HMS Ŀ�ǵ� ���� ����� ���� �ڵ�
#define USER_DEFINE_DEFAULT 100
#define DATACOMMMGR_INDEX -90

enum EVENT_CODE{
	EVENT_UNKNOWN		= 0,
	EVENT_GETDATA		= 1,
	EVENT_UPDATEDATA	= 2,
	EVENT_INSERTDATA	= 3,
	EVENT_DELETEDATA	= 4,
	EVENT_CONTROLAPP	= 5,
	EVENT_BROADCAST		= 6,
	EVENT_ALARM			= 7,
	EVENT_GETDATAASYNC	= 8,
	EVENT_GETDATAASYNCRESULT = 9,
	EVENT_REGHOTLINK	= 10,
	EVENT_UNREGHOTLINK	= 11,
	EVENT_HOTLINK		= 12,
	EVENT_CLEARHOTLINK	= 13,

	EVENT_REGTIMESERIES = 14,
	EVENT_UNREGTIMESERIES = 15,
	//2019-10-01 jsson st
	//��뷮 ���� ���� cpp �۾�
	EVENT_DELIVERYFILE = 16,
	//2019-10-01 jsson ed

	//21.01.06 hyunwoo
	EVENT_GET_EDGE_DATA = 17,

	//21.03.29 hyunwoo	//���Ͻ�Ʈ�� ��� ���� �ݹ��̺�Ʈ �߰�
	EVENT_FILESTREAM = 18,

	EVENT_MAX_COUNT = 200,
};

// controlApp Parameter
enum{
	CMD_UNKNOWN					= 0,
	CMD_ON_CREATE				= 1,
	CMD_ON_SHOW					= 2,
	CMD_ON_HIDE					= 3,
	CMD_ON_DESTROY				= 4,
	CMD_ON_RUN					= 5,
	CMD_ON_PAUSE				= 6,
	CMD_ON_STOP					= 7,
	
	//2021.04.20 HMS st
//	CMD_ON_SHOWMODAL			= 8,
//	CMD_REGIST_SUBSCRIBEDATA	= 9,

    CMD_ON_CHANGE_LAYOUT            = 8,
	//CMD_ON_SHOWMODAL                = 9,
	CMD_ON_OBSERVER                 = 10,
	CMD_ON_BACKUP                   = 11, // Added by Kim,Ga-hee : 2019.05
	CMD_ON_RESTORE                  = 12, // Adde by Kim,Ga-hee : 2019.05 
	//2019-10-01 jsson st
	CMD_ON_DELIVERYFILE             = 13,
	//2019-10-01 jsson ed
	//2021.04.20 HMS ed

	CMD_ON_MAX_COUNT = 200,
};

enum{
	BROADCAST_ON_ALARM			= 0,
	BROADCAST_ON_CHANGE_LAYOUT	= 1,
	BROADCAST_ON_APPDEFINE		= 2,
	BROADCAST_ON_CHANGE_LANGUAGE= 3,
	//2021.04.20 HMS st
	BROADCAST_ON_HIDE           = 4,
	BROADCAST_ON_USER_DEFINE1   = 5,
	BROADCAST_ON_USER_DEFINE2   = 6,
	BROADCAST_ON_USER_DEFINE3   = 7,
	//BROADCAST_ON_APP_ALARM=4,					// Added by Sohn,JinHo : 2018.12.19
	//2021.04.20 HMS ed

	BROADCAST_ON_MAX_COUNT = 200,
};

enum CALLBACK_TYPE
{
	CALLBACK_UNKNOWN			= 0,
	CALLBACK_GETDATA			= 1,
	CALLBACK_UPDATEDATA			= 2,
	CALLBACK_INSERTDATA			= 3,
	CALLBACK_DELETEDATA			= 4,
	CALLBACK_CONTROLAPP			= 5,
	CALLBACK_BROADCAST			= 6,
	CALLBACK_ALARM				= 7,
	CALLBACK_ON_CREATE			= 8,
	CALLBACK_ON_SHOW			= 9,
	CALLBACK_ON_HIDE			= 10,
	CALLBACK_ON_DESTROY			= 11,
	CALLBACK_ON_RUN				= 12,
	CALLBACK_ON_PAUSE			= 13,
	CALLBACK_ON_STOP			= 14,
	CALLBACK_ON_CHANGE_LAYOUT	= 15,
	//CALLBACK_ON_SHOWMODAL		= 16,
	CALLBACK_GETDATAASYNC		= 17,

	//2021.04.20 HMS st
	CALLBACK_ON_GETDATAASYNCRESULT       = 18,
	CALLBACK_ON_SUBSCRIBEDATA            = 19,
	CALLBACK_ON_RESGIST_SUBSCRIBEDATA    = 20,
	CALLBACK_ON_UNREGIST_SUBSCRIBEDATA   = 21,
	CALLBACK_ON_CLEAR_SUBSCRIBEDATA      = 22,
	CALLBACK_ON_OBSERVER                 = 23,

	//2021.04.20 HMS ed

	//2019-10-01 jsson st
	//��뷮 ���� ���� cpp �۾�
	CALLBACK_DELIVERYFILE                = 24,//18,

	//2019-10-01 jsson ed

	CALLBACK_ON_GET_EDGE_DATA            = 25, //Edge Comm. IO Data
	CALLBACK_ON_FILESTREAM               = 26,

	//2021.04.20 HMS ed
	CALLBACK_MAX
};

//2021.04.20 HMS st
enum LANGUAGE
{
	KOREAN = 0, ENGLISH
};

enum LOGLEVEL
{
	LOG_NORMAL = 0,
	LOG_WARN,
	LOG_ERR,
	LOG_FATAL
};

enum XTRACE_LEVEL// level
{
	TRACE_COMMAND_MGR = 0,
	TRACE_COMMUNICATION_MGR,
	TRACE_APPLICATION_MGR,
	TRACE_DATABASE_MGR,
	TRACE_LOG_MGR,
	TRACE_SECURITY_MGR,

	TRACE_LIBRARY = 10000,

	TRACE_APPLICATION = 20000,
};

enum APPHIDE_OPERABLE
{
	False = 0,  //App Hide�� ��� ���� ����
	True        //App Hide�� ��� ���� ����
};

//2021.04.20 HMS ed

enum PERIOD_LEVEL
{
	PERIOD_LV1 = 0,	//1 sampling
	PERIOD_LV2 = 1,	//5 sampling
	PERIOD_LV3 = 2,	//10 sampling
	PERIOD_LV4 = 3,	//15 sampling
	PERIOD_LV5 = 4,	//20 sampling
};

enum FANUC_PLC_TYPE
{
	FANUC_BYTE = 0,
	FANUC_WORD = 1,
	FANUC_LONG = 2,
	FANUC_FLOATING32 = 4,
	FANUC_FLOATING64 = 5
};

enum CSCAM_PLC_TYPE
{ 
	CSCAM_BIT = 0,
	CSCAM_LONG,
	CSCAM_DOUBLE
};

enum KCNC_PLC_TYPE
{
	KCNC_BIT = 0,
	KCNC_LONG,
	KCNC_DOUBLE
};

enum GUD_TYPE
{
	GUD_STRING = 0,
	GUD_DOUBLE
};

//2021.08.11 HMS st manager reset ���� 
enum class MANAGERINFO
{
	MGR_APP = 0,
	MGR_COM = 1,
	MGR_LOG = 2,
	MGR_DB = 3,
	MGR_SECURITY = 4,			// Added by Sohn,JinHo : 2018.07.24 : for Security manager
	MGR_APPALARM = 5,			// Added by Sohn,JinHo : 2018.12.10 : for App Alarm manager 
	MGR_BACKUPRESTORE = 6,		// Added by Kim,Ga-hee : 2019.05 
	//2020-03-26 jsson st
	MGR_INSTALL = 7,			// Added by Kim,Ga-hee : 2019.05 
	//2020-03-26 jsson ed

	MGR_EDGECOM = 8,			// Added by Lee,Hyunwoo : 2021.01.06
	MGR_DEVCOMM = 9,			// Added by Sohn,JinHo : 2022.03.04

	MGR_MAX = 10,
};




