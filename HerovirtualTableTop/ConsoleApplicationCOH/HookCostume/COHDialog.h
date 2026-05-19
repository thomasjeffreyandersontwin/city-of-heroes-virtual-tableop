#pragma once
#include "afxwin.h"
#include "Define.h"

// COHDialog dialog

class COHDialog : public CDialogEx
{
	DECLARE_DYNAMIC(COHDialog)

public:
	COHDialog(CWnd* pParent = NULL);   // standard constructor
	virtual ~COHDialog();

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_DIALOG1 };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedOk();
	CEdit m_NPCINFO;
	CEdit m_command;

	virtual BOOL OnInitDialog();

	virtual LRESULT WindowProc(UINT message, WPARAM wParam, LPARAM lParam);
	CEdit m_mousepos;
	CEdit m_stagepos;
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	CEdit m_SourceXYZ;
	CEdit m_DestXYZ;
	CEdit m_CollisionXYZ;
	afx_msg void OnBnClickedButton1();
	afx_msg void OnBnClickedButton2();
};
