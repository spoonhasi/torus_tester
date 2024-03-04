#pragma once

#include "ApiLib.h"

struct MapAppData;

// �����޸� ���� API
class INTELIGENT_API CAppMap
{
public:
	CAppMap(int executeIndex);
	~CAppMap();

public:
	void PushBroadcast(const char* command);
	const char* PopBroadcast();

private:
	MapAppData* m_map;
};