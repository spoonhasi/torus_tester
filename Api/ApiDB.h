#pragma once

#include "ApiLib.h"

class INTELIGENT_API ApiDB
{
public:
	ApiDB(void);
	~ApiDB(void);

public:
	void Query(const char* query);
};

