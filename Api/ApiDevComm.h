// Created by Sohn,JinHo : 2022.11.26

#pragma once

#include "ApiLib.h"

#include <string>
#include <vector>
#include <unordered_map>

#define MAX_STRING_SIZE	1024	// added by Sohn,JinHo: 2023.02.27

using namespace std;

/// <summary>
/// 
/// </summary>
enum DATATYPE
{
	INBITBLOCK	= 1,
	BITBLOCK	= 2,
	INBYTEBLOCK = 3,
	BYTEBLOCK	= 4,
	INWORDBLOCK = 5,
	WORDBLOCK	= 6,
	INDWORDBLOCK= 7,
	DWORDBLOCK	= 8,
	INQWORDBLOCK= 9,
	QWORDBLOCK	= 10
};

typedef struct
{
	std::string strFilterName;
	int iFilterValue;
}DevCommFilterInfo;

/// <summary>
/// Added by Sohn,JinHo : 2022.11.26
/// </summary>
typedef struct
{
	std::string strMemID;			// Platform Memory ID
	std::string strMemBlock;		// Platform memory block to be mapped with external device PLC memory
	std::string strDataAddress;		// Data address in platform memory block to be mapped with external device PLC memory
	std::string strDevID;			// 
	std::string strDevMemType;		// Device user name
	std::string strDevTargetAdde;	// Password for login the device
	std::string strDescription;		// extern path for device network protocol lib
} DevMemInfo;

/// <summary>
/// Added by Sohn,JinHo : 2022.11.26
/// </summary>
typedef struct
{
	std::string strDevID;
	std::string strMemType;
	std::string strTargetAddr;
	std::string strDesc;
}DevCommAddrInfo;

class INTELIGENT_API CApiDevComm
{
public: 
	CApiDevComm();

	int LoadDevMemInfoFile(char* pFilePath);				// 			
	int swapDevCommAddr(std::string& strFilter);			// 
	int swapDevCommAddr(char* strFilter);
	int initMapFilePath(const char* pFilePath);				// 
	void setDecCommFlag(bool bSetValue);					// 

	
private:
	int pDelSpaceinStr(const char* pInCh, char* pOutCh);	// 
	bool isOnlyNumber(const std::string& str);				// 		
	int searchMemBlock(const char* pChStr);

	std::vector<DevMemInfo> m_DevInfoList;
	std::unordered_map<std::string, DevCommAddrInfo> mapTable;

	bool m_bFlagFieldBusComm;			
	char m_MemMapFilePath[MAX_STRING_SIZE];

	std::vector<DevCommFilterInfo> m_CommFilterInfoList;

};