/*****************************************************
RelationRecord  票据上传医院端、票据端关系表
******************************************************/

drop table if exists RelationRecord;

CREATE TABLE RelationRecord
(
    BILL_MDTRT_INFO_ID   VARCHAR(26)           NOT NULL,            --票据端就诊信息主键 ULID编码
    SETL_BILL_ID         VARCHAR(26)           NOT NULL,            --票据端结算信息主键 ULID编码
    HIS_GHXH             BIGINT                        ,            --医院端门诊挂号序号
    HIS_JSSJH            VARCHAR(50)                   ,            --医院端门诊结算收据号
    HIS_SYXH             BIGINT                        ,            --医院端住院首页序号
    HIS_JSXH             BIGINT                        ,            --医院端住院结算序号
    BILL_TIME            VARCHAR(20)           NOT NULL,            --结算日期 (YYYY-MM-DD hh:mm:ss)
    MDTRT_TYPE           VARCHAR(1)            NOT NULL,            --就诊类型:1门诊;2住院
    RHRED_FLAG           VARCHAR(1)            NOT NULL,            --红冲标志:0未红冲;1已红冲
    SELF_FUNDED_FLAG     VARCHAR(1)            NOT NULL,            --自费标志 0:非自费病人 1:自费病人
    UPLOAD_STAS          VARCHAR(1)            NOT NULL,            --上传状态:1已生成主记录;2已生成明细记录;3已上传主记录;4已上传明细记录
    CRTE_TIME            VARCHAR(20)           NOT NULL,            --创建时间 (YYYY-MM-DD hh:mm:ss)
    UPDT_TIME            VARCHAR(20)                                --更新时间 (YYYY-MM-DD hh:mm:ss)
);


/*****************************************************
MdtrtRecord  票据上传就诊信息表
******************************************************/

drop table if exists MdtrtRecord;

CREATE TABLE MdtrtRecord
(
    BILL_MDTRT_INFO_ID   VARCHAR(26)           NOT NULL,            --票据端就诊信息主键 ULID编码
		BIZ_SN               VARCHAR(50)           NOT NULL,            --就诊流水号
    CERT_NO              VARCHAR(50)                   ,            --证件号码
    PSN_NAME             VARCHAR(50)           NOT NULL,            --人员姓名
    GEND                 VARCHAR(6)                    ,            --性别
    INSUTYPE             VARCHAR(30)                   ,            --险种类型
    HI_NO                VARCHAR(30)                   ,            --医保编号
		HI_ADMDVS_CODE       VARCHAR(6)                    ,            --参保所属医保区划
    BEGNTIME             VARCHAR(20)           NOT NULL,            --开始时间
    ENDTIME              VARCHAR(20)           NOT NULL,            --结束时间
    MED_TYPE             VARCHAR(6)            NOT NULL,            --医疗类别
    MATN_TYPE            VARCHAR(6)                    ,            --生育类别
    BIRCTRL_TYPE         VARCHAR(6)                    ,            --计划生育手术类别
    FETTS                VARCHAR(3)                    ,            --胎次
    GESO_VAL             VARCHAR(3)                    ,            --孕周数
    FETUS_CNT            VARCHAR(3)                    ,            --胎儿数
    DSCG_WAY             VARCHAR(3)                    ,            --离院方式
    DIE_DATE             VARCHAR(20)                   ,            --死亡日期
    IPT_OTP_NO           VARCHAR(30)                   ,            --住院/门诊号
    MEDRCDNO             VARCHAR(30)                   ,            --病历号
    CHFPDR_CODE          VARCHAR(30)                   ,            --主诊医师代码
    CHFPDR_NAME          VARCHAR(30)                   ,            --主诊医师姓名
    ADM_CATY             VARCHAR(10)                   ,            --入院科别
    DSCG_CATY            VARCHAR(10)                                --出院科别
);

/*****************************************************
BillMasterRecord  票据上传结算信息表
******************************************************/

drop table if exists BillMasterRecord;

CREATE TABLE BillMasterRecord
(
    SETL_BILL_ID             VARCHAR(26)           NOT NULL,            --票据端结算信息主键 ULID编码
		BILL_STAS_ID             VARCHAR(26)           NOT NULL,            --票据端结算凭证状态主键 ULID编码
		SETL_RLTS_ID             VARCHAR(26)           NOT NULL,            --票据端结算结算关系主键 ULID编码
    SETL_ID                  VARCHAR(30)                   ,            --结算ID
    MEDFEE_AMT               DECIMAL(16,2)         NOT NULL,            --医疗总费用
    PSN_OWNPAY               DECIMAL(16,2)                 ,            --个人自费
    HIFP_PAY_AMT             DECIMAL(16,2)                 ,            --医保统筹基金支付
    OTH_PAY                  DECIMAL(16,2)                 ,            --其他支付
    PSN_ACCT_PAY             DECIMAL(16,2)                 ,            --个人账户支付
    PSN_CASH_PAY             DECIMAL(16,2)                 ,            --个人现金支付
    PSN_SELFPAY              DECIMAL(16,2)                 ,            --个人自付
		INSCP_AMT                DECIMAL(16,2)                 ,            --符合范围金额
    BILL_CODE                VARCHAR(50)           NOT NULL,            --电子结算凭证代码
    BILL_NO                  VARCHAR(50)           NOT NULL,            --电子结算凭证号码
    BILL_CHKCODE             VARCHAR(20)           NOT NULL,            --电子结算凭证校验码
    BILLER                   VARCHAR(50)                   ,            --开票人
    RECHKER                  VARCHAR(50)                   ,            --复核人
    BILL_AMT                 DECIMAL(16,2)         NOT NULL,            --开票金额
    BILL_DATE                VARCHAR(8)            NOT NULL,            --开票日期
    BILL_TIME                VARCHAR(8)            NOT NULL,            --开票时间
    FILE_URL                 VARCHAR(1000)                 ,            --版式文件链接地址
    REL_ELEC_SETL_CERT_CODE  VARCHAR(50)                   ,            --冲红票原电子结算凭证代码
    REL_ELEC_SETL_CERT_NO    VARCHAR(50)                                --冲红票原电子结算凭证号码
);

/*****************************************************
DiagRecord  票据上传结算信息表
******************************************************/

drop table if exists DiagRecord;

CREATE TABLE DiagRecord
(
    DIAG_INFO_ID         VARCHAR(26)           NOT NULL,            --票据端诊断信息主键 ULID编码
    BILL_MDTRT_INFO_ID   VARCHAR(26)           NOT NULL,            --票据端就诊信息主键 ULID编码
    DIAG_TYPE            VARCHAR(3)                    ,            --诊断类别
    MAINDIAG_FLAG        VARCHAR(3)                    ,            --主诊断标志
    DIAG_CODE            VARCHAR(30)                   ,            --诊断代码
    DIAG_NAME            VARCHAR(255)                  ,            --诊断名称
    DIAG_TIME            VARCHAR(20)                   ,            --诊断时间
    DIAG_DR_CODE         VARCHAR(30)                   ,            --诊断医师代码
    DIAG_DR_NAME         VARCHAR(30)                                --诊断医师姓名
);

/*****************************************************
FeeDetailRecord  票据上传结算明细信息表
******************************************************/

drop table if exists FeeDetailRecord;

CREATE TABLE FeeDetailRecord
(
    FEE_DETL_ID          VARCHAR(26)           NOT NULL,            --票据端结算明细主键 ULID编码
    SETL_BILL_ID         VARCHAR(26)           NOT NULL,            --票据端结算信息主键 ULID编码
    FEE_OCUR_TIME        VARCHAR(20)           NOT NULL,            --费用发生时间
    CNT                  DECIMAL(16,4)         NOT NULL,            --数量
    PRIC                 DECIMAL(16,6)         NOT NULL,            --数量
    DETITEM_FEE_SUMAMT   DECIMAL(16,2)         NOT NULL,            --明细项目费用总额
    MED_CHRGITM_TYPE     VARCHAR(6)                    ,            --医疗收费项目类别
    MEDLIST_CODG         VARCHAR(50)           NOT NULL,            --医疗目录编码
    MEDLIST_NAME         VARCHAR(50)                   ,            --医疗目录名称
    SPEC                 VARCHAR(200)                  ,            --规格
    CHRGITM_LV           VARCHAR(3)                    ,            --收费项目等级
    HOSP_APPR_FLAG       VARCHAR(3)                                 --医院审批标志
);


/*****************************************************
OprnRecord  票据上传手术信息表
******************************************************/

drop table if exists OprnRecord;

CREATE TABLE OprnRecord
(
    OPRN_INFO_ID         VARCHAR(26)           NOT NULL,            --票据端手术主键 ULID编码
    BILL_MDTRT_INFO_ID   VARCHAR(26)           NOT NULL,            --票据端就诊信息主键 ULID编码
    OPRN_OPRT_NAME       VARCHAR(255)                  ,            --手术操作名称
    OPRN_OPRT_CODE       VARCHAR(50)                   ,            --手术操作代码
    MAIN_OPRN_FLAG       VARCHAR(3)                    ,            --主手术操作标志
    OPRN_BEGNTIME        VARCHAR(20)                   ,            --手术开始时间
    OPRN_ENDTIME         VARCHAR(20)                   ,            --手术结束时间
    OPER_DR_NAME         VARCHAR(50)                   ,            --术者医师姓名
    OPER_DR_CODE         VARCHAR(20)                   ,            --术者医师代码
    ANST_DR_NAME         VARCHAR(50)                   ,            --麻醉医师姓名
    ANST_DR_CODE         VARCHAR(20)                   ,            --麻醉医师代码
    ANST_BEGNTIME        VARCHAR(20)                   ,            --麻醉开始时间
    ANST_ENDTIME         VARCHAR(20)                                --麻醉开始时间
);