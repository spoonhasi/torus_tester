#pragma once

#include "ApiLib.h"
#include <guiddef.h>

typedef void* ITEM_HANDLE;
typedef void* RPC_SERVER_HANDLE;
typedef void* RPC_CLIENT_HANDLE;
typedef void* API_HANDLE;

typedef int (*callback_function)(int evt, int cmd, const char* command, char** result);
typedef int (*broadcast_function)(const char* command);

typedef int (*Callback_Apphotlink)(char* result);

#ifdef __cplusplus
extern "C"{
#endif


	INTELIGENT_API API_HANDLE GetApi(GUID appID, const char* appName);
	INTELIGENT_API API_HANDLE GetApiEx(GUID appID, const char* appName, const char* pMapFilePath);	// Added by Sohn,JinHo : 2022.11.26
	//2019-11-20 jsson st
	//Before
	//INTELIGENT_API API_HANDLE ApiInitialize(API_HANDLE api);
	//After
	INTELIGENT_API int ApiInitialize(API_HANDLE api);
	//2019-11-20 jsson ed
	INTELIGENT_API void ApiTerminate(API_HANDLE api);
	INTELIGENT_API void shown(API_HANDLE api);

	INTELIGENT_API int command(API_HANDLE api, const char* cmd, char* result, int maxsize);
	//2020-02-04 jsson st
	INTELIGENT_API int commandArr(API_HANDLE api, char* cmd, char* result, int arrcnt, int maxsize);
	//2020-02-04 jsson ed

	////2021.04.27 HMS st
	INTELIGENT_API int commandTimeout(API_HANDLE api, const char* cmd, char* result, int maxsize, int timeout);
	INTELIGENT_API int commandTimeoutArr(API_HANDLE api, char* cmd, char* result, int arrcnt, int maxsize, int timeout);
	////2021.04.27 HMS ed


	INTELIGENT_API int result_size(API_HANDLE api);
	//2020-02-04 jsson st
	INTELIGENT_API int result_size_arr(API_HANDLE api, int idx);
	//2020-02-04 jsson ed
	INTELIGENT_API const char* result_string(API_HANDLE api);
	//2020-02-04 jsson st
	INTELIGENT_API const char* result_string_arr(API_HANDLE api, int idx);
	//2020-02-04 jsson ed
	INTELIGENT_API void regist_directcallback(API_HANDLE api, callback_function func);
	INTELIGENT_API void regist_typecallback(API_HANDLE api, int type, callback_function func);
	INTELIGENT_API char* allocMem(API_HANDLE api, int size);
	INTELIGENT_API void freeMem(API_HANDLE api, char* mem);

	INTELIGENT_API GUID ApiExecuteId(API_HANDLE api);
	INTELIGENT_API int ApiExecuteIndex(API_HANDLE api);
	INTELIGENT_API const char* ApiAppName(API_HANDLE api);

	INTELIGENT_API int AppInstall(API_HANDLE api, const char* package_file);
	INTELIGENT_API int MakePackageFile(API_HANDLE api, const char* package_dir, const char* output_file);

	INTELIGENT_API void regist_broadcast_callback(API_HANDLE api, broadcast_function func);

	INTELIGENT_API void Trace(int level, const char* msg);

	//2019-10-16 jsson st
	//App call stack 관리 공유메모리 접근
	INTELIGENT_API void setCallStackApp(const char* appName, GUID appID, bool isShow);
	//2019-10-16 jsson ed

	//2019-10-16 jsson st
	//INTELIGENT_API int getCallStackApp(int callindex, const char* appName, GUID* appID, bool* isShow);
	INTELIGENT_API const char* getCallStackAppName(int callindex, GUID* appID, bool* isAval);
	//INTELIGENT_API GUID getCallStackAppID(int callindex);
	//INTELIGENT_API bool getCallStackAppAval(int callindex);
	//2019-10-16 jsson ed

	//2019-10-17 jsson st
	//실행된 App의 리스트 등록/조회/삭제
	//typedef void* APPSTATUS;
	//INTELIGENT_API int ApiAppStatusCount();
	//INTELIGENT_API APPSTATUS ApiAppStatusExeuteApp(int exeindex);
	//INTELIGENT_API int ApiAppStatusRunning(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusRunSignal(APPSTATUS app);
	//INTELIGENT_API const char* ApiAppStatusName(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusInitialized(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusExecuting(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusExecuteIndex(APPSTATUS app);
	//INTELIGENT_API GUID ApiAppStatusExeGuid(APPSTATUS app);
	//INTELIGENT_API GUID ApiAppStatusAppGuid(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusWatchDog(APPSTATUS app);
	//INTELIGENT_API int ApiAppStatusFlag(APPSTATUS app);
	//INTELIGENT_API double ApiAppStatusCPU(APPSTATUS app);
	//INTELIGENT_API double ApiAppStatusRAM(APPSTATUS app);
	//2019-10-17 jsson ed

	//2020-04-09 jsson st
	//INTELIGENT_API void setSiemensLibPath(const char *path);
	//INTELIGENT_API const char* getSiemensLibPath();
	//2020-04-09 jsson ed

	//2021-04-20  HMS st c++ 함수추가
	INTELIGENT_API void setSiemensLibPath(API_HANDLE api, const char* path);
	INTELIGENT_API const char* getSiemensLibPath(API_HANDLE api);
	//2021-04-20  HMS ed

	//2021.10.20 HMS st App간 HotLinkData
	INTELIGENT_API int updateHotLinkData(API_HANDLE api, const char* paramname, const char* data);
	INTELIGENT_API int regAppHotLink(API_HANDLE api, const char* address);
	INTELIGENT_API void regist_apphotlink_callback(API_HANDLE api, Callback_Apphotlink func);
	// Added by Sohn,JinHo : 2022.12.26
	INTELIGENT_API int SwqpFilterData(API_HANDLE api, char* pFilterData);

	//2023.07.18 인코딩

	INTELIGENT_API int result_size_utf8(API_HANDLE api);
	//2020-02-04 jsson st
	INTELIGENT_API int result_size_arr_utf8(API_HANDLE api, int idx);
	//2020-02-04 jsson ed
	INTELIGENT_API const char* result_string_utf8(API_HANDLE api);
	//2020-02-04 jsson st
	INTELIGENT_API const char* result_string_arr_utf8(API_HANDLE api, int idx);
	//2020-02-04 jsson ed


#ifdef __cplusplus
}
#endif