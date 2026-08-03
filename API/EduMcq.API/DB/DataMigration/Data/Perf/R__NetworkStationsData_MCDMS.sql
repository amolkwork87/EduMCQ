--AKS1 8NIs from both Cloud
--AKS2 8NIs from both Cloud but do not start- deployment scale = 0

--Richmond on Prem 4 NIs RichmondP-1N5-MCDM09,RichmondP-1N5-MCDM11,RichmondP-1N5-MCDM13,RichmondP-1N5-MCDM15
--Suwanee on Prem 4 NIs SuwaneeP-1N6-MCDM10,SuwaneeP-1N6-MCDM12,SuwaneeP-1N6-MCDM14,SuwaneeP-1N6-MCDM16

--Richmond Cloud Stations

INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555062','MCDM',201,1,50000,1,'0',NULL,true,'RichmondC-1N5-MCDM01',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555063','MCDM',202,25000,37499,0,'0',NULL,true,'RichmondC-3KS-MCDM03',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555064','MCDM',203,50000,62499,0,'0',NULL,true,'RichmondC-1N5-MCDM05',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555065','MCDM',204,75000,87499,0,'0',NULL,true,'RichmondC-3KS-MCDM07',false,true)  ON CONFLICT DO NOTHING;

--Richmond OnPrem

INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555066','MCDM',205,1,50000,1,'0',NULL,true,'RichmondP-1N5-MCDM09',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555067','MCDM',206,25000,37499,0,'0',NULL,true,'RichmondP-3KS-MCDM11',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555068','MCDM',207,50000,62499,0,'0',NULL,true,'RichmondP-1N5-MCDM13',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555069','MCDM',208,75000,87499,0,'0',NULL,true,'RichmondP-3KS-MCDM15',false,true)  ON CONFLICT DO NOTHING;

--Suwanee Cloud

INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555010','MCDM',209,1,12499,5,'0',NULL,true,'SuwaneeC-1N6-MCDM02',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555011','MCDM',210,37500,49999,37562,'0',NULL,true,'SuwaneeC-38K-MCDM04',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555012','MCDM',211,62500,74999,0,'0',NULL,true,'SuwaneeC-1N6-MCDM06',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555013','MCDM',212,87500,99999,0,'0',NULL,true,'SuwaneeC-38K-MCDM08',false,true) ON CONFLICT DO NOTHING;


--Suwanee On Prem

INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555014','MCDM',213,1,12499,5,'0',NULL,true,'SuwaneeP-1N6-MCDM10',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555015','MCDM',214,37500,49999,37562,'0',NULL,true,'SuwaneeP-38K-MCDM12',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555016','MCDM',215,62500,74999,0,'0',NULL,true,'SuwaneeP-1N6-MCDM14',false,true) ON CONFLICT DO NOTHING;
INSERT INTO network_stations (network_station_id,network_id,station_id,min_approval_code_value,max_approval_code_value,current_approval_code_value,approval_code_format,description,active,new_station_id,byte_array_log_enabled,is_manual_deactivated) VALUES   ('1e1f40e7-6100-4b3e-8236-edfd4a555017','MCDM',216,87500,99999,0,'0',NULL,true,'SuwaneeP-38K-MCDM16',false,true) ON CONFLICT DO NOTHING;
