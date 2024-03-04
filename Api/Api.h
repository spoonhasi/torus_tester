#pragma once

#include "ApiLib.h"
#include "controlcmd.h"
#include "item/document.h"
#include <guiddef.h>
#include <string>
#include "ApiCallbackThread.h"

//2024.02.07 HMS
// Added by Sohn,JinHo : 2022.03.04
// --> 
#include <vector>
//#include <hash_map>
#include <unordered_map>
#include "ApiDevComm.h"

//2023.07.13 HMS Lock �߰� getData, updateData st �ӽ�
#include <mutex>

typedef void* ITEM_HANDLE;
typedef void* RPC_SERVER_HANDLE;
typedef void* RPC_CLIENT_HANDLE;
typedef void* API_HANDLE;

typedef int (*callback_function)(int evt, int cmd, const char* command, char** result);
typedef int (*broadcast_function)(const char* command);
//2019-10-08 jsson st
typedef int (*callback_function_data)(int evt, int cmd, const char* command, char** result, void *data);
//2019-10-08 jsson ed

//typedef int (*getDataAsyncCallback_func)(ITEM_HANDLE item);
typedef int (*Callback_Apphotlink)(char* result);


//class FocasApi;
struct _AppHotLinkData;
struct ApphotlinkmapData;


/**
@breif Application���� ����ϴ� ��� Api�� �����Ѵ�.
@mainpage
**/
class INTELIGENT_API CApi
{
private:
	CApi(GUID appID, const char* appName);
	CApi(GUID appID, const char* appName, const char* pFilePath);	// Added by Sohn,JinHo : 2022.03.04

public:
	~CApi(void);

	static CApi* Get(GUID appID, const char* appName);
	static CApi* Get(const char* appID, const char* appName);
	static CApi* Get(GUID appID, const char* appName, const char* pMapFilePath);	// Added by Sohn,JinHo : 2022.11.26
	static CApi* Get();

public:
	//FocasApi* focas;
	

public:
	static CApi* initialize(GUID appid, const char* appName);
	static CApi* initialize(const char* appid, const char* appName);

	//Added by cscam : 2024.01.04 TORUS 2.2
	static CApi* initialize(GUID appid, const char* appname, const char* strmapfilepath);

public:
	int initialize();

	int terminate();

	int getData(const char* address, const char* filter, ITEM_HANDLE result, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);
	//int getData(const char* address, const char* filter, double *dData, bool direct = false);
	//double getData2(const char* address, const char* filter,bool direct = false);
	//int getData(const char* address, const char* filter,bool direct = false);

	//21.04.20 hyunwoo
	int getData(const char** address, const char** filter, int arrcnt, ITEM_HANDLE result, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);

	//21.04.21 hyunwoo
	int getData(const char* address, const char* filter, ITEM_HANDLE result, PERIOD_LEVEL level, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);
	int getData(const char** address, const char** filter, int arrcnt, ITEM_HANDLE result, PERIOD_LEVEL level, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);
	int getDataEX(const char* address, const char* filter, ITEM_HANDLE result, PERIOD_LEVEL level, bool bAppHideOperable = false, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);
	int getDataEX(const char** address, const char** filter, int arrcnt, ITEM_HANDLE result, PERIOD_LEVEL level, bool bAppHideOperable = false, bool direct = true, int timeout = API_TIMEOUT_DEFAULT);

	//int getData(getDataItem[] item);
	int updateData(const char* address, const char* filter, ITEM_HANDLE data, ITEM_HANDLE item, int timeout = API_TIMEOUT_DEFAULT);

	//21.04.22 hyunwoo
	int updateData(const char** address, const char** filter, int arrcnt, ITEM_HANDLE data, ITEM_HANDLE item, int timeout = API_TIMEOUT_DEFAULT);
	int updateDataEX(const char* address, const char* filter, ITEM_HANDLE data, ITEM_HANDLE item, bool bAppHideOperable = false, int timeout = API_TIMEOUT_DEFAULT);
	int updateDataEX(const char** address, const char** filter, int arrcnt, ITEM_HANDLE data, ITEM_HANDLE item, bool bAppHideOperable = false, int timeout = API_TIMEOUT_DEFAULT);

	//2021.04.28 HMS st c#�� ���� ����.
	//int getDataAsync(const char* address, const char* filter, ITEM_HANDLE result);
	int getDataAsync(const char* address, const char* filter);
	//2021.04.28 HMS ed

	int getDataAsyncResult(const char* address, ITEM_HANDLE result);


	
	//2024.02.06 HMS comment add st TORUS 2.2
	//C# ���� Ȯ���ؾ� �Ѵ�.
	// 
	//- GUI App�� �ε� ������ �÷����� Ȯ���ϴ� ����
	//- ���հ��� �������� Code Generation �ÿ� �ڵ����� ���Ե�
	//- ���� �ʿ���
	//- ���� �� �Լ� ����, �÷����� GUI App�� �ε� ������ Ȯ���� �� �ִٸ� �����ص� ��

	void GuiLoaded();

	//2024.02.06 HMS add
	//���� ����� �߰��� �־�� ��. 
	//- GUI�� ���� App�� ���, ���� ������ �÷����� Ȯ���� �� �ִ� ����� 
	//(�ƹ��͵� ���ϰ� ������ �����ؾ� ��)
	//(���� ���� �������� Code generation �ÿ� �ڵ����� ���Ե�)
	//�÷����� �� �Լ� ���̵� GUI App�� ���� ������ ��Ȯ�� �ľ��� �� �ִٸ� �����ص� ��

	void GuiClosed();

	//2024.02.06 HMS comment add ed TORUS 2.2

	//void ModalFinish(ITEM_HANDLE result);

	int insertData(const char* address, ITEM_HANDLE data, ITEM_HANDLE result, int timeout = API_TIMEOUT_DEFAULT);
	int deleteData(const char* address, ITEM_HANDLE data, ITEM_HANDLE result, int timeout = API_TIMEOUT_DEFAULT);

	int controlApp(int cmd, const char* address, ITEM_HANDLE item, ITEM_HANDLE result, int timeout = API_TIMEOUT_DEFAULT);
	int broadcast(int cmd, ITEM_HANDLE item);

	//2021.04.20 HMS st
	int subscribeData(const char* addr, const char* filter);
	int subscribeDataResult(const char* addr, ITEM_HANDLE data);
	int unsubscribeData(const char* addr, const char* filter);
	int subscribeDataClear();
	//2021.04.20 HMS ed

	//2022.07.15 HMS Object ����
	//int subscribeData(const char* addr, const char* filter, ITEM_HANDLE obj);
	int subscribeData(const char* addr, const char* filter, int* objectID);// ITEM_HANDLE obj);
	int unsubscribeData(int objectID);

	void regist_callback(int callbackid, callback_function func) {m_callback[callbackid] = func;}

	int getMachinesInfo(ITEM_HANDLE result);

#if 0
	int getPlcSignal(int dataType, const char* plcAddress, ITEM_HANDLE result);
	int setPlcSignal(int dataType, const char* plcAddress, ITEM_HANDLE data, ITEM_HANDLE result);
#else

	//2021.08.12 HMS st machine=0 �� ����, STDAPI ������ ����Ǵ� ����. 
    //machine �� �Ѱ��� ���� �Ǿ� ���� ���� ���ڿ� ������� ����
    //�Ѱ��� ���� �Ǿ� �־ machineList �� ���ڴ�� ���� ���� 
  // 
	//21.04.22 hyunwoo
	//Fanuc
	int getPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, bool direct, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, ITEM_HANDLE data, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, int* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, double* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(FANUC_PLC_TYPE dataType, const char* startAddress, const char* endAddress, char** data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//Simens
	int getPlcSignal(const char* plcAddress, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getPlcSignal(const char* plcAddress, bool direct, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(const char* plcAddress, ITEM_HANDLE data, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(const char* plcAddress, int* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(const char* plcAddress, double* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(const char* plcAddress, char** data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//CSCAM, KCNC
	int getPlcSignal(int dataType, int count, const char* startAddress, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getPlcSignal(int dataType, int count, const char* startAddress, bool direct, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(int dataType, int count, const char* startAddress, ITEM_HANDLE data, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(int dataType, int count, const char* startAddress, int* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(int dataType, int count, const char* startAddress, double* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPlcSignal(int dataType, int count, const char* startAddress, char** data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);


#endif

	int getToolOffsetData(int channel, int number, int type, bool direct, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getToolOffsetData(const char* channel, const char* number, const char* type, bool direct, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//21.04.22 hyunwoo
	int setToolOffsetData(const char* channel, const char* number, const char* type, int* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setToolOffsetData(const char* channel, const char* number, const char* type, double* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setToolOffsetData(const char* channel, const char* number, const char* type, char** data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setToolOffsetData(int channel, int number, int type, int* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setToolOffsetData(int channel, int number, int type, double* data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setToolOffsetData(int channel, int number, int type, char** data, int dataCnt, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);


	int getAttributeExists(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeType(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeIsNc(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeLogicalPath(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeName(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributePath(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeSize(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getAttributeEditedTime(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getFileList(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//2021.11.25 HMS st file�� ������ ��� �������� �Լ�
	int getFileListEx(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2021.11.25 HMS ed

	int CreateCNCFile(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CreateCNCFolder(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileRename(const char* path, const char* name, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileCopy(const char* source, const char* target, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileMove(const char* source, const char* target, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileDelete(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileDeleteAll(const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileExecute(int channel, const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CNCFileExecuteExtern(int channel, const char* path, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//2023.02.11 HMS add st
	int CreateCNCFile(const char* parentpath, const char* name, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int CreateCNCFolder(const char* parentpath, const char* name, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2023.02.11 HMS add ed
	
	//21.04.22 hyunwoo
	//21.09.16 HMS st Stdapi ���� �����Ǿ� ������, DataCode.xml �� ����,
	//TORUS 1.3 ���� ������ �����Ͽ� �����ϱ�� ��. with  ����ȣ å��. 
	//int ToolLoading(int toolarea, const char* tools, int location, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	// data(const char*)�� NC File�� �����ϴ�.
	int setFileStream(const char* path, const char* data, int machine = 0);

	// NC File�� data(const char*)�� �����ɴϴ�.
	// data�� �ݵ�� delete�Ǿ���մϴ�.

	//21.05.03 hyunwoo st
	int getFileStream(const char* path, int& resLineCount, int machine = 0);
	int getFileStreamData(char** resData);
	//21.05.03 hyunwoo ed
	// NC File�� data(const char*)�� �����ɴϴ�.
	// bufsize��ŭ�� �����͸� �����ɴϴ�.
	//int getFileStream(const char* data, int bufsize, const char* path);

	// 2019-06-19 �߰�
	int createTool(int toolArea, int tools, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int deleteTool(int toolArea, int tools, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int loadTool(int toolArea, int tools, int location, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int unloadTool(int toolArea, int tools, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int setPassword(int userAccount, const char* password, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int logInAccount(const char* password, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int logOutAccount(ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int selectBlock(int channel, int blockCounter, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int searchBlock(int channel, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2023.07.11 HMS st groupGCode  ����
    //Channel��  ��ü Gmodal �ڵ� ������ ���������� ������.
	int getGModal(int channel, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2021.05.17 HMS st  __ groupGCode �߰�
	//2024.02.05 HMS TORUS 2.2 ����
	//int getExModal(int channel, int groupGCode, const char* modal, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2021.05.17 HMS ed

	//2022.02.11 HMS st
	int getExModal(int channel, const char* modal, ITEM_HANDLE result, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	//2022.02.11 HMS ed

	int deleteMDI(int channel, ITEM_HANDLE item, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	
	int setMDIString(int channel, const char* data, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);


	//int getMDIString(int channel, const char** data, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);
	int getMDIString(int channel, int& resLineCount, int machine = 0, int timeout = API_TIMEOUT_DEFAULT);

	//2021.04.16 HMS st 
	int sendLog(const char* msg, ITEM_HANDLE result);
	int sendLogEx(const char* msg, ITEM_HANDLE result, int loglevel);
	int setLanguage(int language, ITEM_HANDLE result);
    //2021.04.16 HMS ed
    //2021.04.19 HMS st
	int registTimeSeries(const char* address, const char* filter, ITEM_HANDLE result);
	int unregistTimeSeries(const char* address, const char* filter);

	int DeliveryFile(const char* appname, const char* path, int timeout = API_TIMEOUT_DEFAULT);

	//2021.04.28 HMS st machine=1 �� ���� , 04.30 HMS st channel �߰�
    //2022.07.22 HMS timeout ����
	//Timeout ������ ��ȯ�� ���Ŀ��� STDAPI���� ���μ����� ������� �ʰ� ������ ���������� �Ϸ��.
	//STDAPI���� grpc cancel�� ������ �� �ִ� ����� �������� Timeout �ð��� �����ϱ�� ��. 
	int UploadFile(const char* local_path, const char* nc_path, int machine = 0, int channel = 1, int timeout = 60000);//API_TIMEOUT_DEFAULT);
	int DownloadFile(const char* nc_path, const char* local_path, int machine = 0, int channel = 1, int timeout = 60000);//API_TIMEOUT_DEFAULT);
	//2021.04.28 HMS ed

	void setSiemenseLibPath(const char* path);
	const char* getSiemenseLibPath();

	//2021.04.20 HMS ed

	//2022.03.04 HMS st gud �� operation �Լ� �߰�
	//2024.02.06 HMS st �Լ� �ϳ��� ����

	//int getGudData(GUD_TYPE dataType, int number, int count, const char* address, ITEM_HANDLE result, int machine = 0, int channel = 1, int timeout = API_TIMEOUT_DEFAULT);
	int getGudData(GUD_TYPE dataType, int number, int count, const char* address, bool direct, ITEM_HANDLE result, int machine = 0, int channel = 1, int timeout = API_TIMEOUT_DEFAULT);

	int setGudData(GUD_TYPE dataType, int number, int count, const char* address, ITEM_HANDLE data, ITEM_HANDLE result, int machine = 0, int channel = 1, int timeout = API_TIMEOUT_DEFAULT);
	int setGudData(int number, int count, const char* address, double* data, ITEM_HANDLE result, int machine = 0, int channel = 1, int timeout = API_TIMEOUT_DEFAULT);
	int setGudData(int number, int count, const char* address, char** data, ITEM_HANDLE result, int machine = 0, int channel = 1, int timeout = API_TIMEOUT_DEFAULT);

	//2022.03.04 HMS ed

	void AddTraceMsg(int level, const char* szMessage);

	//���� ��� �Լ�
public:

	void shown();	// gui�� �����.

	//2020.01.06 HMS App singletone ���� check �߰�
	int guishown();

	int command(const char* command, char** result);
	//2020-02-04 jsson st
	int command(const char* command, int arrcnt, char** result);
	//2020-02-04 jsson ed

	//2021.04.26 HMS st _ grpc Timeout �߰�
	int command(const char* command, char** result, int timeout);
	int command(const char* command, int arrcnt, int timeout, char** result);
	//2021.04.26 HMS ed 

	int subscribeData(const char* addr, callback_function observer);
	int unsubscribeData(callback_function observer);

	void OnBroadCast(const char* cmd);
	void broadcast_callback(int id, broadcast_function func) { m_broadcast_callback[id] = func; }

	//2019-10-08 jsson st
	void regist_callback_data(callback_function_data func) { m_callbackData = func; }
	//2019-10-08 jsson ed

	//c#�� directcallback���θ� �Ѿ�°� ����. hyunwoo
	void regist_directcallback(callback_function func) { m_directcallback = func; }

	//2020-03-10 jsson st
	void callback_thread(const std::string tname, int num);
	//2020-03-10 jsson ed

	int updateHotLinkData(const char* paramname, ITEM_HANDLE data);
	int updateHotLinkDataProc(const char* paramname, const char* data);

	//2021.07.28 HMS st
	void OnHotLinkCompare();
	int regAppHotLink(const char* address);
	void AppHotLinkData_callback(Callback_Apphotlink func) { m_appHotlink_callbback = func; };
	//

	//2022.10.06 HMS st
	void OnAsyncDataResult();

#if false
	//2021.08.11 HMS st
	int Reset(MANAGERINFO mgrinfo);
	int Reconnection();
	//2021.08.11 HMS ed

#endif

public: // callback event
	static int callback(const unsigned char *cmd,unsigned char **result);


protected:

	int updateData(const char* address, const char* filter, const char* data, ITEM_HANDLE resultItem, int timeout = API_TIMEOUT_DEFAULT);

	int getDataProc(const char* address, const char* filter, ITEM_HANDLE result, bool direct, bool bAppHideOperableOption, PERIOD_LEVEL periodLv, int timeout);
	int getDataArrayProc(const char** address, const char** filter, int arrcnt, ITEM_HANDLE result, bool direct, bool bAppHideOperableOption, PERIOD_LEVEL periodLv, int timeout);

	int updateDataProc(const char* address, const char* filter, ITEM_HANDLE data, ITEM_HANDLE resultItem, bool bAppHideOperable, int timeout);
	int updateDataProc(const char* address, const char* filter, const char* data, ITEM_HANDLE resultItem, bool bAppHideOperable, int timeout);
	int updateDataArrayProc(const char** address, const char** filter, int arrcnt, ITEM_HANDLE data, ITEM_HANDLE resultItem, bool bAppHideOperable, int timeout);

	void RegistCallbackThreadFunc();

	//2021.07.28 HMS st
	
	void OnRegistAppHotLinkData(ApphotlinkmapData* mapdata);
	bool AppcheckProc();

public: // property
	const char* appName(){return m_appName;}
	GUID appID(){return m_appID;}
	int executeIndex(){return m_executeIndex;}
	GUID executeID(){return m_executeID;}
	void executeIDString(char* result);
	RPC_CLIENT_HANDLE getClient(){return m_rpcClient;}
	
	//ANSI
	int lastResultSize();
	const char* lastResult();

	//2020-02-04 jsson st
	int lastResultSize(int idx);
	const char* lastResult(int idx);
	//2020-02-04 jsson ed

	//2023.07.18 HMS .net 5.0 �̻��� UTF8 �� ���� 
	//encoding ANSI->UTF8����
	int lastResultSizeUTF8();
	char* lastResultUTF8();

	//2020-02-04 jsson st
	int lastResultSizeUTF8(int idx);
	char* lastResultUTF8(int idx);
	//2020-02-04 jsson ed


	bool AppStop(){return m_appstop;}

	int iSwapFilterAddrData(char* pChFilterData);	// Added by Sohn,JinHo : 2022.12.26
	

private:
	static CApi* m_api;

	std::mutex getDataProc_mutex;
	std::mutex updateDataProc_mutex;
	
private:
	GUID m_appID;
	char m_appName[1024];
	int m_executeIndex;
	GUID m_executeID;

	callback_function m_callback[CALLBACK_MAX];
	callback_function m_directcallback;
	broadcast_function m_broadcast_callback[BROADCAST_MAX];
	//2019-10-08 jsson st
	callback_function_data m_callbackData;
	//2019-10-08 jsson ed



	RPC_SERVER_HANDLE m_rpcServer;
	RPC_CLIENT_HANDLE m_rpcClient;

	char* m_lastResult;
	//2020-02-04 jsson st
	char** m_lastResultArr;
	int		  m_lastResultArrCnt;
	//2020-02-04 jsson ed
	bool m_appstop;

	//21.04.23 hyunwoo
	callback_thread_function m_callbackThread[CALLBACK_MAX];

	CApiDevComm m_DevCommApi; // Added by Sohn,JinHo : 2022.11.26

	// Added by Sohn,JinHo : security manager Function

	char m_streamFilePath[256];

	//2021.07.28 HMS st
	
	Callback_Apphotlink  m_appHotlink_callbback;

	//Callback_AsyncDataResult m_AsyncDataresult_callback;	

	//2022.010.06 HMS asyncdata event ��� ��ȯ�� 
	int getDataAsyncResult(const char* address, const char* result);


};
